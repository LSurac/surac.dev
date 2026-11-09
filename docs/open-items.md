# Open items

What is still open. Only open lines stay here: a finished item is deleted, and what it taught goes
into the document it belongs to (`docs/decisions.md`, `docs/ops-and-deploy.md`, …).

## Acceptance on staging (maintainer)

The staging content was entered on 2026-11-08 and accepted by the maintainer the same day. Still
open in the admin:

- [ ] "Über mich": a chess photo, once one exists (Immich had only screenshots of games).

## Manual steps (maintainer)

- [ ] VPS provider: confirm the data processing agreement for this account is signed. The privacy
      page already names dogado GmbH, Dortmund, and states that the agreement exists.
- [ ] Search Console: add the domain property `surac.dev` (DNS TXT verification), submit
      `https://surac.dev/sitemap.xml`.
- [ ] Production admin: decide whether to keep the password copied over with the staging content
      or change it in the admin (rotates the session everywhere).
- [ ] Current professional photo (web size and print size); staging uses the photo from the CV.
- [ ] GitHub: pin this repository and set the profile's website to `https://surac.dev` (the CLI
      token lacks the `user` scope: `gh auth refresh -h github.com -s user`, or in the browser).
      The profile README exists (`LSurac/LSurac`).

## Production setup (live since 2026-11-09)

- [ ] Telemetry: a Seq ingest key for production, then `SURACDEV_OTLP_ENDPOINT`/`_HEADERS` in the
      host's `.env`; check that traces arrive as `deployment.environment.name = production`.
- [ ] healthchecks.io dead-man's switch for the backup timer: the account is at its check limit,
      so the check could not be created yet; `HEALTHCHECK_URL` stays empty until then.
- [ ] Staging refresh from production (the homelab's refresh service is being generalised per
      site).

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
