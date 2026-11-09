# Host assets

Installed by hand on the production host, not by `deploy.ps1` (docs/ops-and-deploy.md).

```bash
sudo install -m 644 surac-dev.caddy /etc/caddy/sites/surac-dev.caddy
sudo caddy validate --config /etc/caddy/Caddyfile && sudo systemctl reload caddy
sudo install -m 755 surac-dev-backup.sh /usr/local/bin/surac-dev-backup
sudo install -m 755 surac-dev-healthwatch.sh /usr/local/bin/surac-dev-healthwatch
sudo install -m 644 surac-dev-backup.service surac-dev-backup.timer surac-dev-healthwatch.service surac-dev-healthwatch.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now surac-dev-backup.timer surac-dev-healthwatch.timer
```

`surac-dev.caddy` is a site block only. The host is shared with other sites, so
`/etc/caddy/Caddyfile` belongs to the host: it holds the global options
(`servers { trusted_proxies static private_ranges }`) and `import sites/*.caddy`.

`/etc/surac-dev-backup.env` (mode 600, owner `deploy`) holds `HEALTHCHECK_URL` and `NTFY_URL`. It is
created by hand and never committed.
