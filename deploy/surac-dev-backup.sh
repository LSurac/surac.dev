#!/usr/bin/env bash
# Nightly backup on the production host (docs/ops-and-deploy.md, "Backups"): database dump and the
# documents volume into /opt/surac-dev-backups, 14 daily sets, success ping to healthchecks.io,
# failure alert via ntfy. Installed as /usr/local/bin/surac-dev-backup with the .service/.timer
# next to it. Secrets come from /etc/surac-dev-backup.env (HEALTHCHECK_URL, NTFY_URL), never from here.
set -euo pipefail

STACK_DIR="${STACK_DIR:-/opt/surac-dev}"
BACKUP_DIR="${BACKUP_DIR:-/opt/surac-dev-backups}"
KEEP_DAYS="${KEEP_DAYS:-14}"
ENV_FILE="/etc/surac-dev-backup.env"
STAMP="$(date +%Y%m%d-%H%M%S)"

# shellcheck disable=SC1090
[ -f "$ENV_FILE" ] && . "$ENV_FILE"

notify_failure() {
  if [ -n "${NTFY_URL:-}" ]; then
    curl -fsS -m 10 -H "Title: surac.dev backup failed" -H "Priority: high" -d "$(hostname): backup $STAMP failed at step: $1" "$NTFY_URL" || true
  fi
  if [ -n "${HEALTHCHECK_URL:-}" ]; then
    curl -fsS -m 10 "$HEALTHCHECK_URL/fail" >/dev/null || true
  fi
}

step="prepare"
trap 'notify_failure "$step"' ERR

mkdir -p "$BACKUP_DIR/db" "$BACKUP_DIR/documents"
cd "$STACK_DIR"

step="pg_dump"
docker compose exec -T surac-dev-db pg_dump -Fc -U suracdev suracdev > "$BACKUP_DIR/db/suracdev-$STAMP.dump"

step="documents"
docker run --rm --volumes-from surac-dev-api -v "$BACKUP_DIR/documents:/backup" alpine:3 \
  tar -czf "/backup/documents-$STAMP.tar.gz" -C /documents .

step="manifest"
(cd "$BACKUP_DIR" && sha256sum "db/suracdev-$STAMP.dump" "documents/documents-$STAMP.tar.gz" > latest.sha256)

step="rotate"
find "$BACKUP_DIR/db" -name 'suracdev-*.dump' -mtime +"$KEEP_DAYS" -delete
find "$BACKUP_DIR/documents" -name 'documents-*.tar.gz' -mtime +"$KEEP_DAYS" -delete

step="ping"
if [ -n "${HEALTHCHECK_URL:-}" ]; then
  curl -fsS -m 10 "$HEALTHCHECK_URL" >/dev/null
fi

echo "surac.dev backup $STAMP ok"
