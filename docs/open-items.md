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
- [x] Client address behind Caddy: the API now reads `X-Forwarded-For` through its own hops
      (loopback, Docker networks), so every visitor gets their own rate-limit partition in
      production too (2026-11-08, `ClientAddressTests`).

## Engineering

- [x] Phase 1 domains (Profile, Career, Projects, Skills, Legal) with translations and admin editors.
- [x] Documents: upload, public photo and case-study images, admin management.
- [x] Public start page composed from `ui/` components, both locales; case study, legal pages, 404.
- [x] Open Graph tags, sitemap (`/sitemap.xml`, both locales) and `robots.txt`.
- [x] E2E flows 1 and 2 (start page with CV download, admin edit shown on the site).
- [x] QuestPDF CV `Public` × de/en with golden-file tests and the download on the start page.
- [x] CV `Full` variant (phone, address) for application links.
- [x] Application links and the mini ATS: admin API and UI, `/b/<token>` view in both locales,
      link events, statistics page, E2E flows 3 and 4 (phase 2, 2026-11-08).
- [ ] Interests page and complete English translations (phase 3).
- [ ] CSP hash/nonce for Angular's SSR inline state; document in gotchas once solved.
- [ ] Initial bundle is 392.7 kB against a 320 kB warning and 400 kB error budget (385 kB before
      phase 2; the lazy link page shares `HomePage` with the eager start page, which costs chunk
      overhead, and an eager link route measured worse at 395 kB). Angular's FESM packages put
      everything the lazy routes use from core/common into the shared initial chunk. Only about
      7 kB headroom left: address before the next feature that touches the start page, at the
      latest in the phase 3 performance pass.
- [ ] Lines over 110 characters in C#: `.editorconfig` sets 110, the API code does not follow it
      consistently (foundation included). Decide: enforce with a formatting pass, or set a C#
      limit that matches practice.
- [ ] Decide whether `traktor-library-tools` becomes a second public repository (secret scan,
      README rewrite).
