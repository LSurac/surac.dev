# Operations and deployment

Hosting, environments, CI/CD, backups, monitoring, telemetry. Access facts (hosts, IPs, users,
keys) are deliberately absent: they live in the maintainer's private tooling, and the workflows read
them from repository variables and secrets.

## Environments

| | Staging | Production |
|---|---|---|
| Host | homelab server (LAN + Tailscale only) | VPS on the maintainer's own account, Debian 13, KVM |
| Branch | `dev` | `master` |
| URL | internal, port 8085 | `https://surac.dev` |
| TLS | none (internal) | Caddy on the host, Let's Encrypt |
| Database | shared development PostgreSQL instance, database `suracdev` | `surac-dev-db` container (`postgres:17-alpine`), named volume |
| Compose profile | none | `prod` |
| Telemetry | none | OTLP to Seq via the private network |

One `compose.yaml` for both. Production enables the database container and the OTLP exporter via
`COMPOSE_PROFILES=prod` and `.env` keys; staging points at the shared instance.

## Containers

| Container | Image | Memory | Notes |
|---|---|---|---|
| `surac-dev-api` | `SuracDev.Api/Dockerfile` (sdk → aspnet:10.0) | 384 MB | internal only, port 8080 |
| `surac-dev-web` | `SuracDev.Web/Dockerfile` (node build → nginx + node runtime) | 256 MB | nginx on 8080 proxies `/api/` to the API and everything else to the SSR process on 4000; published on `127.0.0.1:<port>` |
| `surac-dev-db` | `postgres:17-alpine` | 256 MB | `prod` profile only, volume `surac-dev-pgdata` |

Volumes: `surac-dev-dpkeys` (Data Protection), `surac-dev-documents` (uploads), `surac-dev-pgdata`
(production database). All services: `restart: unless-stopped`, json-file logging capped at
10 MB × 3, non-root users, `cap_drop: ALL`, `no-new-privileges`.

## Deploy

`deploy.ps1` is the single deploy path, for humans and CI alike:

```powershell
.\deploy.ps1 -TargetHost <host> -TargetUser <user> -RemotePath /opt/surac-dev [-SkipLocalBuild] [-Production]
```

It runs the local gate (unless skipped because CI ran it), stages the working tree without
`node_modules`, `.git`, `.env` and build output, uploads a tarball, keeps the host's `.env`, and runs
`docker compose up -d --build --wait`. With `-Production` it refuses to run from a dirty tree or a
branch other than `master`, dumps the database before the deploy (`pg_dump -Fc`) and aborts if the
dump fails.

Workflows (`.github/workflows/`):

- `gates.yml` — every push and pull request: API build, tests with coverage threshold, Web lint,
  tests with coverage threshold, SSR build, NSwag contract check, gitleaks. Linux runners, no secrets.
- `deploy.yml` — push to `master`: gates, E2E, then `deploy.ps1 -Production` from a Windows runner
  over SSH to `${{ vars.PROD_HOST }}`. Host key pinned from `${{ vars.PROD_HOST_KEY }}`.
- `deploy-staging.yml` — push to `dev`: gates, then deploy over Tailscale to
  `${{ vars.STAGING_HOST }}`.
- `dependency-audit.yml` — monthly `npm audit --omit=dev --audit-level=high` and
  `dotnet list package --vulnerable --include-transitive`.

The deploy jobs run only when the environment's switch is on (`vars.STAGING_DEPLOY_ENABLED`,
`vars.PROD_DEPLOY_ENABLED` set to `true`) and its host variable exists; otherwise they are skipped,
not failed. Turn a switch on only after the host variables, the host key and all secrets of that
environment are in place.

Actions are pinned by commit SHA. Workflow permissions are `contents: read`. GitHub Actions minutes
are finite; when they run out, `deploy.ps1` from the workstation is the documented fallback, never
both at once.

## Production host

Set up from the maintainer's existing VPS runbook (same provider family and OS as another project):
Debian 13, `deploy` user with key-only SSH and passwordless sudo, sshd hardening drop-in, ufw (22,
80, 443), unattended-upgrades with a 05:30 reboot window, Docker CE from Docker's repository
(`apt-mark hold`), Caddy from Caddy's repository, Tailscale for the telemetry path, journald
persistent, logrotate.

The VPS is shared with the maintainer's other private sites. Host-wide configuration (packages,
firewall, users, Tailscale, `/etc/caddy/Caddyfile` with the global options and
`import sites/*.caddy`) belongs to the host, not to this repository. This project owns its paths
only: `/opt/surac-dev`, `/opt/surac-dev-backups`, its `surac-dev-*` units and its site block.

`deploy/surac-dev.caddy` is installed to `/etc/caddy/sites/surac-dev.caddy` by hand (not by
`deploy.ps1`): apex site, `www` redirect, security headers, reverse proxy to `127.0.0.1:<port>`,
access log with 14-day rotation. It has no global block; `trusted_proxies` comes from the host's
Caddyfile.

## Backups

`deploy/surac-dev-backup.sh` + `surac-dev-backup.timer` (03:33 daily on the host):

1. `pg_dump -Fc` of the production database into `/opt/surac-dev-backups/db/`.
2. `tar` of the documents volume into `/opt/surac-dev-backups/documents/`.
3. Keep 14 daily sets locally; write a `latest.sha256`.
4. Ping a healthchecks.io check on success; failure alerts via ntfy.

The homelab pulls `/opt/surac-dev-backups/` read-only (forced-command SSH key) at 05:00 into the
existing offsite chain (encrypted rclone to cloud storage). The pull is
`deploy/pi-pull-prod-backups.sh` (own read-only key, alerts when the newest dump is older than 48 h).
A restore drill runs monthly, `deploy/pi-restore-drill.sh`: it restores the newest dump into a
throwaway PostgreSQL without network, compares every table's row count with the dump's own data
and checks the documents archive. Both scripts install on the homelab side, not on production.
The procedure was rehearsed on staging on 2026-11-08: the nightly dump restored into a throwaway
`postgres:17-alpine` container without network and matched the live row counts.

## Monitoring

- Uptime monitor on `https://surac.dev/api/health` (keyword `ok`, 60 s interval) and a TLS expiry
  check, from the maintainer's Uptime Kuma.
- `deploy/surac-dev-healthwatch.{sh,timer}` on the host: every 5 minutes, `docker compose ps` must
  show all services healthy; otherwise one ntfy alert per state change.
- healthchecks.io dead-man's switch for the backup timer.

## Telemetry

OpenTelemetry in the API: ASP.NET Core, HttpClient and Npgsql traces, logs and metrics over
OTLP/HTTP (protobuf) to the maintainer's Seq (`<seq>/ingest/otlp`) over the private network, tagged
`service.name = surac-dev-api`, `deployment.environment.name = production`. Enabled only when
`OTEL_EXPORTER_OTLP_ENDPOINT` is set; local, tests and staging run without it. Spans never carry
request bodies or cookies; application-link tokens are replaced by `{token}` in every path that
reaches telemetry (`TelemetryRedaction`). ASP.NET Core's own request logs are below the `Warning`
level and are not exported.

Usage events, in the same shape as the maintainer's other sites so the Seq dashboards
("surac.dev · Übersicht/Besucher/Betrieb/Metriken") are shared code:

- `RequestLoggingMiddleware`: one line per proxied API request (`{RequestMethod} {RequestPath}
  answered {StatusCode} in {ElapsedMs} ms for {UserName} ({UserRole}) from {ClientIp}.`) for every
  request through nginx's `/api/` location (marked by its `X-Real-IP`); the SSR process's own calls
  carry the page's X-Forwarded-For but no X-Real-IP and stay at Debug.
- `PageViewsController` (`POST /api/public/page-views`, anonymous, own rate limit) fed by the
  browser's `PageViewTracker`: `Page view {PagePath} ({PageArea}) by ... via {ReferrerHost}.` Areas
  `public`, `link` (`/b/{token}`), `admin`. Automated browsers send nothing.
- Login attempts: `Login succeeded ...` / `Failed login ... {FailureReason}` with the full address.
- Metrics: the framework meters (hosting, Kestrel, rate limiting, diagnostics, authentication, HTTP,
  runtime, Npgsql) plus `SuracDev.Site` (`suracdev.page_views` by area, `suracdev.logins` by outcome),
  delta temporality.

Anonymous visitors appear with the network part of their address only (IPv4 /24, IPv6 /48,
`ClientAddress`); the signed-in admin and login attempts with the full address. Seq keeps 30 days.

## Reminders

On Mondays from 07:00 UTC the API sends one ntfy message listing every application still `Sent`
fourteen days after `AppliedOn` (`docs/decisions.md`). `.env` sets `SURACDEV_NTFY_SERVER`,
`SURACDEV_NTFY_TOPIC` and, for a protected topic, `SURACDEV_NTFY_TOKEN`; without server and topic
the worker is not registered. Staging and production use the maintainer's existing ntfy topic on a
public ntfy server, reached directly over HTTPS (values copied host to host from the homelab's
monitor configuration, never written down).

## Staging specifics

The staging stack is registered in the homelab's stack list so the existing smoke test, health
check, config backup and update jobs pick it up automatically. The database `suracdev` with its own
role is created once by hand on the shared instance and is dumped by the homelab's nightly database
backup like every other database there. The two named volumes (uploaded documents, Data Protection
keys) are not bind mounts, so the homelab's config backup and rebuild export name them explicitly;
a new volume in `compose.yaml` has to be added there too. The homelab's Pi Dev Center journal
receives a deployment event from the staging workflow.

## Runbook pointers

- First production deploy: create `.env` from `.env.example` on the host, install the site block,
  run `deploy.ps1 -Production -TargetHost …`, log in once to set the admin password, enter content,
  upload the photo, generate and check both PDFs, create the first application link, verify the
  backup ran and the uptime monitor is green.
- Rotate the admin session everywhere: change the password in the admin (rotates the security
  stamp).
- Lost admin password: `UPDATE admin_users SET "PasswordHash" = NULL` on the host, next login sets
  a new one.
