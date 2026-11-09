#!/bin/bash
# Runs ON THE PI (not the production host). Pulls the production stack's local backups (/opt/surac-dev-backups on the
# production host: db/, documents/) into the homelab backup tree, so the existing homelab offsite job carries
# them on (rclone crypt). Production keeps its own 14 daily sets regardless.
#
# Install: /usr/local/bin/surac-dev-prod-backup-pull (0755, root:root)
# Config:  /etc/surac-dev-prod-pull.conf (0600, root) with SURACDEV_PROD_TARGET="<user>@<host>" - the host is an
#          infrastructure fact and stays out of this repository (AGENTS.md).
# Needs:   /root/.ssh/surac-dev-prod-pull_ed25519 - a key of its own that the production host restricts to read-only
#          access to /opt/surac-dev-backups/ (rrsync -ro), never the deploy key: the deploy user is in the docker group and
#          therefore root-equivalent on production. Plus the host in /root/.ssh/known_hosts.
# Cron:    via pi-dev-center-reporting-job (job key pi-surac-dev-prod-pull) at 05:00. The production backup runs at 03:33;
#          the homelab offsite sync at 05:30.
set -euo pipefail

CONF="/etc/surac-dev-prod-pull.conf"
# shellcheck disable=SC1090
[ -f "$CONF" ] && . "$CONF"
: "${SURACDEV_PROD_TARGET:?SURACDEV_PROD_TARGET is not set in $CONF}"

# "/" is the root of the rrsync-restricted directory, i.e. /opt/surac-dev-backups/ on the production host.
REMOTE="${SURACDEV_PROD_TARGET}:/"
DEST="/opt/backups/homelab/surac-dev-prod/"
KEY="/root/.ssh/surac-dev-prod-pull_ed25519"
MAX_DUMP_AGE_HOURS=48

alert() {
	echo "$2" >&2
	if [ -x /usr/local/bin/notify-ntfy ]; then
		echo "$2" | /usr/local/bin/notify-ntfy "$1" high warning || true
	fi
}

mkdir -p "$DEST"
chmod 700 "$DEST"

# No --delete (homelab backup policy). Production prunes to 14 days; the mirror keeps 30 and prunes itself below.
# --no-owner/--no-group: the production deploy user's uid means nothing here; the mirror belongs to root.
if ! rsync -a --no-owner --no-group --timeout=60 --exclude='*.partial' \
	-e "ssh -i $KEY -o IdentitiesOnly=yes -o BatchMode=yes" \
	"$REMOTE" "$DEST"; then
	alert "surac.dev prod backup pull FAILED" "Could not pull the surac.dev production backups."
	exit 1
fi

# Freshness before cleanup: the cleanup is by age alone. If the production timer stopped, the pull would stay green
# while the mirror deletes itself down to nothing, so a run without a fresh dump is an error and deletes nothing.
if [ -z "$(find "${DEST}db" -type f -name 'suracdev-*.dump' -mmin -$((MAX_DUMP_AGE_HOURS * 60)) -print -quit 2>/dev/null)" ]; then
	alert "surac.dev prod backups are STALE" "No surac.dev production dump younger than ${MAX_DUMP_AGE_HOURS}h - is surac-dev-backup.timer running?"
	exit 1
fi

find "$DEST" -type f \( -name '*.dump' -o -name '*.tar.gz' \) -mtime +30 -delete

echo "pulled: $(find "$DEST" -type f | wc -l) files, $(du -sh "$DEST" | cut -f1) in $DEST"
