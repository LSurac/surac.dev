# Open items

What is in flight or waiting on a manual step. Close items here when done; the phases live in
`docs/roadmap.md`.

## Done (foundation, 2026-11-07)

- [x] Repository public, `master` protected (four required checks, linear history), `dev` branch.
- [x] Staging: database and role on the shared development PostgreSQL, stack registered, port
      8085, uptime monitor, first deploy by hand, then by CI from `dev` over the private network.
- [x] Repository variables and secrets for staging, including the Tailscale OAuth client;
      `STAGING_DEPLOY_ENABLED` is on.
- [x] gitleaks installed locally, pre-commit hook active.
- [x] Domain `surac.dev` registered and the production VPS ordered (own account).

## Manual steps (maintainer)

- [ ] VPS provider: sign the data processing agreement; wait for the order to appear in the panel.
- [ ] Cloudflare: add the zone `surac.dev` (free plan), switch the nameservers at the registrar,
      keep records "DNS only"; a zone-scoped API token for the maintainer's DNS tooling.
- [ ] Cloudflare Email Routing: `leonard@surac.dev` → private mailbox.
- [ ] Current professional photo (web size and print size).
- [ ] Decide the English wording of the title line and the fact bar labels.
- [ ] GitHub profile README, pin this repository, bio link to `surac.dev` (at launch).

## Production setup (once the VPS is reachable)

- [ ] Harden the host per the maintainer's VPS runbook, install Caddy, Tailscale (`tag:prod`),
      Docker, the backup and healthwatch timers (`deploy/README.md`).
- [ ] `.env` with the `prod` profile, repository variables `PROD_HOST`, `PROD_HOST_KEY`, secret
      `PROD_DEPLOY_SSH_KEY`, DNS records, first `deploy.ps1 -Production`, then `PROD_DEPLOY_ENABLED`.
- [ ] Backup pull into the maintainer's offsite chain, uptime monitor, telemetry API key.

## Engineering

- [ ] Phase 1 domains (Profile, Career, Projects, Skills, Legal) with translations and admin editors.
- [ ] Public start page composed from `ui/` components, both locales.
- [ ] QuestPDF CV documents (Public/Full × de/en) with golden-file tests.
- [ ] Application links and the mini ATS (phase 2).
- [ ] Interests page and complete English translations (phase 3).
- [ ] CSP hash/nonce for Angular's SSR inline state; document in gotchas once solved.
- [ ] Decide whether `traktor-library-tools` becomes a second public repository (secret scan,
      README rewrite).
