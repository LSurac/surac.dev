#!/bin/bash
# Runs ON THE PI (not the production host). Monthly restore drill for the surac.dev production backups that
# pi-pull-prod-backups.sh mirrors into /opt/backups/homelab/surac-dev-prod/: a backup nobody ever restored is a hope, not a
# backup. It takes the NEWEST file of each kind, never touches the production host or the homelab's own databases,
# and reports by ntfy (success at normal priority, any failure at high):
#   1. Freshness: the dump is younger than 48 h.
#   2. Database: restores the dump into a throwaway postgres:17-alpine container (data in tmpfs, no published port,
#      removed on exit) and compares, for every table in the dump, the restored row count with the rows in the dump's
#      own COPY block. A restore that "succeeds" but skips data fails here.
#   3. Files: extracts the newest documents archive into a scratch directory and compares the extracted file count
#      with the archive listing; it must contain files. (The Data Protection key ring is not backed up: a restore
#      only signs the admin out.)
#
# Install: /usr/local/bin/surac-dev-restore-drill (0755, root:root)
# Cron:    0 7 3 * *  (monthly, 3rd at 07:00; the other sites' drills run on the 1st and 2nd, the pull at 05:00)
set -euo pipefail

SRC="/opt/backups/homelab/surac-dev-prod"
IMAGE="postgres:17-alpine"
CONTAINER="surac-dev-restore-drill"
DB_USER="suracdev"
DB_NAME="suracdev"
MAX_DUMP_AGE_HOURS=48

work="$(mktemp -d /tmp/surac-dev-restore-drill.XXXXXX)"
report=()

notify() {
	local title="$1" priority="$2" tag="$3" msg="$4"
	echo "$title: $msg"
	if [ -x /usr/local/bin/notify-ntfy ]; then
		echo "$msg" | /usr/local/bin/notify-ntfy "$title" "$priority" "$tag" || true
	fi
}

fail() {
	notify "surac.dev restore drill FAILED" high warning "$1"
	exit 1
}

cleanup() {
	docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
	rm -rf "$work"
}
trap cleanup EXIT
trap 'fail "restore drill aborted unexpectedly (line $LINENO)"' ERR

newest() { ls -1t "$SRC"/$1 2>/dev/null | head -n 1 || true; }

dump="$(newest 'db/suracdev-*.dump')"
documents="$(newest 'documents/documents-*.tar.gz')"

[ -n "$dump" ] || fail "no db/suracdev-*.dump in $SRC"
[ -n "$documents" ] || fail "no documents archive in $SRC"
[ -n "$(find "$dump" -mmin "-$((MAX_DUMP_AGE_HOURS * 60))" -print)" ] || fail "newest dump $(basename "$dump") is older than ${MAX_DUMP_AGE_HOURS} h"

# --- database ---
password="$(head -c 24 /dev/urandom | base64 | tr -dc 'A-Za-z0-9')"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" --memory 384m --tmpfs /var/lib/postgresql/data \
	-e POSTGRES_DB="$DB_NAME" -e POSTGRES_USER="$DB_USER" -e POSTGRES_PASSWORD="$password" \
	"$IMAGE" >/dev/null

ready=
for _ in $(seq 1 60); do
	if docker exec "$CONTAINER" pg_isready -U "$DB_USER" -d "$DB_NAME" >/dev/null 2>&1; then
		ready=1
		break
	fi
	sleep 2
done
[ -n "$ready" ] || fail "the throwaway postgres did not become ready"
# pg_isready answers during the entrypoint's temporary server too; wait for the final one.
sleep 5
docker exec "$CONTAINER" pg_isready -U "$DB_USER" -d "$DB_NAME" >/dev/null || fail "the throwaway postgres is not ready"

docker cp "$dump" "$CONTAINER:/tmp/drill.dump"
# pg_restore exits non-zero for harmless "already exists" errors too, so the real errors are counted instead.
docker exec "$CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --no-owner /tmp/drill.dump 2> "$work/restore.err" || true
restore_errors="$(grep '^pg_restore: error:' "$work/restore.err" | grep -vc 'already exists' || true)"
[ "$restore_errors" = 0 ] || fail "pg_restore of $(basename "$dump") reported $restore_errors errors, first: $(grep -m1 '^pg_restore: error:' "$work/restore.err")"

# Rows per table as the dump itself holds them: one line per row inside each COPY block.
docker exec "$CONTAINER" pg_restore -f - --data-only /tmp/drill.dump | awk '
	/^COPY / { table = $2; rows = 0; inCopy = 1; next }
	inCopy && /^\\\.$/ { print table, rows; inCopy = 0; next }
	inCopy { rows++ }' > "$work/dump-counts.txt"
[ -s "$work/dump-counts.txt" ] || fail "no table data found in $(basename "$dump")"

tables=0
mismatches=""
while read -r table expected; do
	actual="$(docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -Atc "SELECT count(*) FROM $table")"
	tables=$((tables + 1))
	if [ "$actual" != "$expected" ]; then
		mismatches+="${mismatches:+, }$table (dump $expected, restored $actual)"
	fi
done < "$work/dump-counts.txt"
[ -z "$mismatches" ] || fail "row counts differ: $mismatches"

migrations="$(docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -Atc 'SELECT count(*) FROM "__EFMigrationsHistory"')"
[ "${migrations:-0}" -gt 0 ] || fail "the restored database has no EF migration history"
report+=("database: $tables tables match, $migrations migrations ($(basename "$dump"))")

# --- files ---
extract_check() {
	local label="$1" archive="$2" dir="$work/$1" listed extracted
	mkdir -p "$dir"
	tar -xzf "$archive" -C "$dir" || fail "$(basename "$archive") cannot be extracted"
	listed="$(tar -tzf "$archive" | { grep -v '/$' || true; } | sort -u | wc -l)"
	extracted="$(find "$dir" -type f | wc -l)"
	[ "$listed" = "$extracted" ] || fail "$label: the archive lists $listed files but $extracted were extracted"
	[ "$extracted" -gt 0 ] || fail "$label: the archive contains no files"
	report+=("$label: $extracted files")
}

extract_check documents "$documents"

notify "surac.dev restore drill OK" default white_check_mark "$(printf '%s; ' "${report[@]}" | sed 's/; $//')"
trap - ERR
