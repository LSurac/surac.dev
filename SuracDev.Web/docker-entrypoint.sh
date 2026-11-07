#!/bin/sh
# Runs the Angular SSR process next to nginx in one container. Whichever exits first takes the
# container down, so `restart: unless-stopped` restarts the pair instead of leaving nginx proxying
# to a dead upstream.
set -e

API_BASE_URL="${API_BASE_URL:-http://surac-dev-api:8080}" PORT=4000 node /srv/surac-dev/server/server.mjs &
ssr_pid=$!

nginx -g 'daemon off;' &
nginx_pid=$!

# QUIT for nginx (graceful; the base image's STOPSIGNAL), TERM for node.
trap 'kill -QUIT "$nginx_pid" 2>/dev/null; kill -TERM "$ssr_pid" 2>/dev/null; wait "$ssr_pid" "$nginx_pid" || true; exit 0' TERM INT QUIT

while kill -0 "$ssr_pid" 2>/dev/null && kill -0 "$nginx_pid" 2>/dev/null; do
  sleep 5
done

echo "surac-dev-web: nginx or the SSR process exited, stopping the container" >&2
kill -TERM "$ssr_pid" "$nginx_pid" 2>/dev/null || true
exit 1
