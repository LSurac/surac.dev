# Roadmap

Phases are cut so that later phases add UI and content, not schema changes. The schema of all
entities in `docs/content-model.md` ships with phase 1.

## Phase 0 — Foundation (this)

Repository, docs, solution, API skeleton (Identity, Health, shared infrastructure, first
migration), Angular SSR skeleton with tokens, fonts, layout shell and the first `ui/` components,
Compose, `deploy.ps1`, CI gates, gitleaks, Playwright scaffold. Repository goes public at the end of
this phase.

## Phase 1 — The CV, online

- Domains Profile, Career (Experience, Education), Projects, Skills, Legal with translations.
- Admin: login, editors for all of the above with de/en side by side, photo upload.
- Public: start page (hero, fact bar, profile, featured projects, career, skills, "Arbeitsweise"),
  case study page, legal pages, locale switch, SEO basics (title, description, Open Graph,
  `hreflang`, sitemap, robots).
- `GET /api/public/cv.pdf` (`Public` variant, de/en) via QuestPDF; golden-file tests.
- Staging deploy from `dev`, production deploy from `master`, backups, monitoring, telemetry.
- Launch checklist below.

## Phase 2 — Applications

- Documents (certificates, cover letters) with `LinkOnly` visibility.
- Application and ApplicationLink with profiles, greeting, expiry, revoke, full PDF unlock.
- Public `/b/<token>` view with profile-specific section order.
- Link events and the statistics page (per application: opened, downloads, time to first open;
  overall: response rate, status pipeline).
- E2E flows 3 and 4.

## Phase 3 — Personality and polish

- Interests ("Über mich") page with images.
- Complete English translations and English PDF review by a native speaker.
- Curated code excerpts in case studies.
- Performance pass: LCP under 1.5 s on a mid-range phone, initial bundle under 150 kB, Lighthouse
  100/100/100/100 for the start page.
- ntfy reminders for applications without a reply after 14 days (optional).

## Later, maybe

Storybook for `ui/`, second public repository, blog once three articles exist, 2FA if the threat
model changes.

## Launch checklist (end of phase 1)

- [ ] Both PDFs open in Acrobat, Chrome and a phone; copy-paste yields text in reading order; file
      names `Lebenslauf_Leonard_Surac.pdf` / `CV_Leonard_Surac.pdf`.
- [ ] Start page passes a 30-second recruiter read: role, stack, years, location, PDF and e-mail
      without scrolling on desktop and phone.
- [ ] Every link works; spell check in both languages by a second person.
- [ ] Impressum and privacy page complete and reachable in two clicks.
- [ ] Lighthouse accessibility 100; keyboard walk-through of every page.
- [ ] Uptime monitor green for 7 days; backup restored once on a scratch database.
- [ ] GitHub: repository public, profile README, pinned, bio link.
- [ ] CV PDF in the application folder replaced by the generated one.
