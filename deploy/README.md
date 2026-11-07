# Host assets

Installed by hand on the production host, not by `deploy.ps1` (docs/ops-and-deploy.md).

```bash
sudo install -m 644 Caddyfile /etc/caddy/Caddyfile && sudo systemctl reload caddy
sudo install -m 755 surac-dev-backup.sh /usr/local/bin/surac-dev-backup
sudo install -m 755 surac-dev-healthwatch.sh /usr/local/bin/surac-dev-healthwatch
sudo install -m 644 surac-dev-backup.service surac-dev-backup.timer surac-dev-healthwatch.service surac-dev-healthwatch.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now surac-dev-backup.timer surac-dev-healthwatch.timer
```

`/etc/surac-dev-backup.env` (mode 600, owner `deploy`) holds `HEALTHCHECK_URL` and `NTFY_URL`. It is
created by hand and never committed.
