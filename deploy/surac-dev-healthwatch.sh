#!/usr/bin/env bash
# Every five minutes: all services of the stack must be running and healthy; otherwise one ntfy
# alert per state change (a marker file prevents repeats). NTFY_URL comes from
# /etc/surac-dev-backup.env, shared with the backup script.
set -euo pipefail

STACK_DIR="${STACK_DIR:-/opt/surac-dev}"
STATE_FILE="/var/tmp/surac-dev-healthwatch.state"
ENV_FILE="/etc/surac-dev-backup.env"

# shellcheck disable=SC1090
[ -f "$ENV_FILE" ] && . "$ENV_FILE"

cd "$STACK_DIR"
unhealthy="$(docker compose ps --format '{{.Name}} {{.Status}}' | grep -Ev '\(healthy\)' || true)"
previous="$(cat "$STATE_FILE" 2>/dev/null || echo ok)"

if [ -n "$unhealthy" ]; then
  if [ "$previous" = "ok" ] && [ -n "${NTFY_URL:-}" ]; then
    curl -fsS -m 10 -H "Title: surac.dev unhealthy" -H "Priority: high" -d "$unhealthy" "$NTFY_URL" || true
  fi
  echo "degraded" > "$STATE_FILE"
  exit 0
fi

if [ "$previous" != "ok" ] && [ -n "${NTFY_URL:-}" ]; then
  curl -fsS -m 10 -H "Title: surac.dev healthy again" -d "all services healthy" "$NTFY_URL" || true
fi
echo "ok" > "$STATE_FILE"
