# Open items

What is in flight or waiting on a manual step. Close items here when done; the phases live in
`docs/roadmap.md`.

## Manual steps (maintainer)

- [ ] Register `surac.dev` (Cloudflare Registrar), zone on Cloudflare, DNS only (no proxy).
- [ ] Cloudflare Email Routing: `leonard@surac.dev` → private mailbox.
- [ ] Own VPS account (not the club's), data processing agreement, server S-class, Debian 13.
- [ ] Repository variables `PROD_HOST`, `PROD_HOST_KEY`, `STAGING_HOST`, `STAGING_HOST_KEY`;
      secrets `PROD_DEPLOY_SSH_KEY`, `STAGING_DEPLOY_SSH_KEY`, `TS_OAUTH_CLIENT_ID`, `TS_OAUTH_SECRET`.
- [ ] Staging database `suracdev` and role on the shared development PostgreSQL; stack registered
      in the homelab stack list; port 8085.
- [ ] Current professional photo (web size and print size).
- [ ] Decide the English wording of the title line and the fact bar labels.
- [ ] Make the repository public once gitleaks, branch protection and README are in place.
- [ ] GitHub profile README, pin this repository, bio link to `surac.dev`.

## Engineering

- [ ] Phase 1 domains (Profile, Career, Projects, Skills, Legal) with admin editors.
- [ ] Public start page composed from `ui/` components, both locales.
- [ ] QuestPDF CV documents (Public/Full × de/en) with golden-file tests.
- [ ] Application links and the mini ATS (phase 2).
- [ ] Interests page and complete English translations (phase 3).
- [ ] CSP hash/nonce for Angular's SSR inline state; document in gotchas once solved.
- [ ] Decide whether `traktor-library-tools` becomes a second public repository (secret scan,
      README rewrite).
