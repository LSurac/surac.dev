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
- [ ] Current professional photo (web size and print size); staging uses the photo from the CV.
- [ ] GitHub: pin this repository and set the profile's website to `https://surac.dev` (the CLI
      token lacks the `user` scope: `gh auth refresh -h github.com -s user`, or in the browser).
      The profile README exists (`LSurac/LSurac`).

## Production setup (live since 2026-11-09)


## Launch checks (after go-live)

- [ ] Both PDFs open in Acrobat and on a phone (file names, embedded fonts and the text order were
      checked on 2026-11-09).
- [ ] Spell check in both languages by a second person; English PDF reviewed by a native speaker.
- [ ] LCP target 1.5 s: production measures 2.0 to 2.5 s in Lighthouse's throttled mobile profile
      (2026-11-09, after HTTPS/HTTP/2; performance 97 to 98, CLS 0, accessibility/best practices/SEO 100
      on all 16 pages). Decide whether the target stands for the throttled profile.
- [ ] Uptime monitor green for 7 days.
- [ ] CV PDF in the application folder replaced by the generated one.
