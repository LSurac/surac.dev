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

Actions are pinned by commit SHA. Workflow permissions are `contents: read`. GitHub Actions minutes
are finite; when they run out, `deploy.ps1` from the workstation is the documented fallback, never
both at once.

## Production host

Set up from the maintainer's existing VPS runbook (same provider family and OS as another project):
Debian 13, `deploy` user with key-only SSH and passwordless sudo, sshd hardening drop-in, ufw (22,
80, 443), unattended-upgrades with a 05:30 reboot window, Docker CE from Docker's repository
(`apt-mark hold`), Caddy from Caddy's repository, Tailscale for the telemetry path, journald
persistent, logrotate.

`deploy/Caddyfile` is installed to `/etc/caddy/Caddyfile` by hand (not by `deploy.ps1`): apex
site, `www` redirect, security headers, reverse proxy to `127.0.0.1:<port>`, access log with 14-day
rotation.

## Backups

`deploy/surac-dev-backup.sh` + `surac-dev-backup.timer` (03:33 daily on the host):

1. `pg_dump -Fc` of the production database into `/opt/surac-dev-backups/db/`.
2. `tar` of the documents volume into `/opt/surac-dev-backups/documents/`.
3. Keep 14 daily sets locally; write a `latest.sha256`.
4. Ping a healthchecks.io check on success; failure alerts via ntfy.

The homelab pulls `/opt/surac-dev-backups/` read-only (forced-command SSH key) at 05:00 into the
existing offsite chain (encrypted rclone to cloud storage). A restore drill runs monthly: restore the
latest dump into a scratch PostgreSQL, run `MigrateAsync`, count rows, compare with the manifest.

## Monitoring

- Uptime monitor on `https://surac.dev/api/health` (keyword `ok`, 60 s interval) and a TLS expiry
  check, from the maintainer's Uptime Kuma.
- `deploy/surac-dev-healthwatch.{sh,timer}` on the host: every 5 minutes, `docker compose ps` must
  show all services healthy; otherwise one ntfy alert per state change.
- healthchecks.io dead-man's switch for the backup timer.

## Telemetry

OpenTelemetry in the API: ASP.NET Core and HttpClient instrumentation, OTLP/gRPC exporter to the
maintainer's Seq over the private network. Enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set;
local, tests and staging run without it. Spans never carry request bodies, cookies, tokens or
link slugs (the link controller redacts the route value). Logs follow the same path with a
redaction processor for the token route parameter.

## Staging specifics

The staging stack is registered in the homelab's stack list so the existing smoke test, health
check, config backup and update jobs pick it up automatically. The database `suracdev` with its own
role is created once by hand on the shared instance. The homelab's Pi Dev Center journal receives a
deployment event from the staging workflow.

## Runbook pointers

- First production deploy: create `.env` from `.env.example` on the host, install the Caddyfile,
  run `deploy.ps1 -Production -TargetHost …`, log in once to set the admin password, enter content,
  upload the photo, generate and check both PDFs, create the first application link, verify the
  backup ran and the uptime monitor is green.
- Rotate the admin session everywhere: change the password in the admin (rotates the security
  stamp).
- Lost admin password: `UPDATE admin_users SET "PasswordHash" = NULL` on the host, next login sets
  a new one.
