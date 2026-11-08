# Open items

What is still open. Only open lines stay here: a finished item is deleted, and what it taught goes
into the document it belongs to (`docs/decisions.md`, `docs/ops-and-deploy.md`, …).

## Acceptance on staging (maintainer)

The staging content was entered on 2026-11-08 from the 2026 CV, the reference letters, the
maintainer's work notes, the project repositories and the cover letters. To check and complete in
the admin:

- [ ] Texts in both languages: profile, career, projects and case studies, "Über mich", the
      "Arbeitsweise" section (`SuracDev.Web/src/app/core/i18n/ui-strings.ts`). The case studies are
      written in the first person as problem, solution, implementation.
- [ ] Würth IT station: 1Plus and the sales backend services are the main work, the other bullets
      are side projects from the work notes; location is empty. Confirm or trim.
- [ ] Fact bar: "8 own applications in production" counts SK Sulzfeld, Kassensturz, Pi Dev Center,
      Cycle Tracker, Trading Lab, Music Sorter, the homelab launcher and surac.dev itself. Confirm
      the count or name fewer. The vocational school cell now shows the certificate's average 1,3
      "with award" (it said 1,0; only the subject grades are all "sehr gut").
- [ ] Abitur entry: the CV has only the year; start date is a placeholder (07/2016).
- [ ] LinkedIn URL, availability line (both empty).
- [ ] Orga-Soft: only the apprenticeship reference exists (Paperless has no separate reference
      letter for 06–10/2024). Request one if wanted; AERA's final reference is uploaded.
- [ ] "Über mich": a chess photo is missing (Immich had only screenshots of games).
- [ ] Admin password additionally in Vaultwarden? (today only in Credential Manager
      `Claude:SuracDevAdmin`).

## Manual steps (maintainer)

- [ ] VPS provider: sign the data processing agreement; wait for the order to appear in the panel.
      The privacy page already names dogado GmbH, Dortmund, and states that the agreement exists.
- [ ] Cloudflare: add the zone `surac.dev` (free plan), switch the nameservers at the registrar,
      keep records "DNS only"; a zone-scoped API token for the maintainer's DNS tooling.
- [ ] Cloudflare Email Routing: `leonard@surac.dev` → private mailbox. The site already shows this
      address.
- [ ] Current professional photo (web size and print size); staging uses the photo from the CV.
- [ ] GitHub: pin this repository and set the profile's website to `https://surac.dev` (the CLI
      token lacks the `user` scope: `gh auth refresh -h github.com -s user`, or in the browser).
      The profile README exists (`LSurac/LSurac`).

## Production setup (once the VPS is reachable)

- [ ] Harden the host per the maintainer's VPS runbook, install Caddy, Tailscale (`tag:prod`),
      Docker, the backup and healthwatch timers (`deploy/README.md`).
- [ ] `.env` with the `prod` profile, repository variables `PROD_HOST`, `PROD_HOST_KEY`, secret
      `PROD_DEPLOY_SSH_KEY`, DNS records, first `deploy.ps1 -Production`, then `PROD_DEPLOY_ENABLED`.
- [ ] Backup pull into the maintainer's offsite chain, uptime monitor, telemetry API key, ntfy
      route for the reminders.
- [ ] Restore drill once on production (rehearsed on staging, `docs/ops-and-deploy.md`).

## Launch checks (after go-live)

- [ ] Both PDFs open in Acrobat, Chrome and a phone; copy-paste yields text in reading order; file
      names `Lebenslauf_Leonard_Surac.pdf` / `CV_Leonard_Surac.pdf`.
- [ ] Start page passes a 30-second recruiter read on desktop and phone.
- [ ] Every link works; spell check in both languages by a second person; English PDF reviewed by
      a native speaker.
- [ ] Keyboard walk-through of every page (Lighthouse accessibility is 100 on all public pages).
- [ ] Lighthouse on production with HTTPS and HTTP/2; LCP target 1.5 s (staging: 2.7 s over
      HTTP/1.1 without TLS, `docs/decisions.md`).
- [ ] Uptime monitor green for 7 days.
- [ ] CV PDF in the application folder replaced by the generated one.
