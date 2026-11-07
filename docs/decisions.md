# Decisions

Settled trade-offs from the planning session of 2026-11-07 and after. Each entry says what was
decided, what was rejected, and why. Breaking these is the most likely failure mode of delegated
work, because most of them look like improvements and are not. New decisions are appended with
their date; reversed decisions are struck through, never deleted.

## Purpose and audience

- **The site serves two readers, and the recruiter decides the first screen.** Research
  (`docs/research/recruiter-perspective.md`) shows HR spends well under a minute, pattern-matches
  keywords, and wants the PDF and the contact data without scrolling. Hiring managers read GitHub,
  not the site. So the public start page is HR-first: name, role, stack line, years, location, two
  buttons (CV PDF, e-mail), links to GitHub and LinkedIn. Depth (projects, case studies, how I work)
  follows below. Rejected: a tech-first layout that opens with projects.
- **One site, three audience profiles, identical content.** An application link
  (`/b/<slug>`) carries a profile (`Public`, `Recruiter`, `TechLead`) that changes section order,
  the greeting line and the unlocked downloads. Profiles never add or hide facts. Rejected: two
  separate sites; token-dependent content, which would look manipulative the moment HR and a tech
  lead compare notes.
- **The public site stays open.** No site-wide token. The GitHub bio, the e-mail signature, a
  printed CV and search engines need an open URL, and access hurdles are a documented negative with
  reviewers. Application links unlock the complete PDF (phone, address), the certificates and the
  cover letter; the open site offers a reduced PDF with e-mail contact only.
- **The job title stays the search term**: "Fullstack-Entwickler · .NET & Angular". "Agentic" is
  a section ("Arbeitsweise") and a README paragraph, not a title: nobody searches for "Agentic
  Software Engineer" in 2026. The section describes the working mode briefly and factually (plan,
  rules, what the agents do, what stays with me) and points to this repository as the evidence.
  It does not argue against anything; the repository speaks for itself.
- **German first, English complete.** Target employers are German; the CV is German. English is a
  full second locale (URL prefix `/en`, translation tables, PDF in both languages from the same
  data), not a half-translated afterthought. Code, README, commits and docs are English.
- **No blog in v1.** Three strong posts beat thirty weak ones, and zero posts beat a page that says
  "coming soon". Revisit when at least three finished articles exist.
- **A separate "Über mich" page carries personality** (DJ and producer as Leonard Lichtblick,
  co-founder of the Sanctuary of Sound collective, chess and SK Sulzfeld, the homelab, family and
  cats). Not on the start page. Partner stays unnamed; that is her decision, not this site's.
- **Projects are shown as case studies, not repositories.** The maintainer's other repositories are
  private and stay private (club website with member data, personal finance). A case study has
  problem, architecture sketch, two or three decisions with trade-offs, screenshots, stack, own
  share, status, and optionally curated code excerpts with commentary.

## Architecture

- **Full stack on purpose, kept proportional.** A static site with a CI-built PDF would be the
  cheaper product. This repository is also the work sample for a .NET/Angular full-stack role, so it
  uses the production stack: ASP.NET Core API, EF Core on PostgreSQL, Angular SSR, cookie auth, PDF
  generation, Docker Compose behind Caddy. The discipline that keeps it from becoming the
  "over-engineered portfolio site" reviewers reject: no feature without a real user (the maintainer as
  admin, or the recruiter), and every structural choice justified here or in `docs/architecture.md`.
- **DDD-light, not tactical DDD.** Bounded contexts are domain folders. Entities have private
  setters, factory methods and behaviour methods that enforce invariants. Value objects exist where
  they prevent a class of bugs (`Locale`, `Slug`). Application services are the use cases and use EF
  Core directly. Rejected: aggregates with repositories, domain events infrastructure, MediatR, unit
  of work, CQRS. Eight entities do not justify that ceremony, and saying so is part of the work
  sample.
- **Component-driven Angular without Storybook (v1).** `ui/` holds presentational components with
  inputs and outputs only; `public/` and `admin/` compose them. A dev-only `/styleguide` route renders
  every `ui/` component with its variants. Storybook may follow once the site is live; it was
  rejected for v1 as a heavy dependency with its own build pipeline.
- **Translation tables, not JSON columns.** Each translatable entity has a `*_translations` table
  keyed by (`<entity>_id`, `locale`). Structural fields (dates, order, links, visibility) live once on
  the entity. English falls back to German with a visible "untranslated" marker in the admin.
  Rejected: `jsonb` i18n columns (poor querying, ugly EF mapping) and a separate English content
  copy (drift).
- **QuestPDF renders the CV**, server-side, deterministic, no browser in the container. The PDF is
  deliberately *not* a print of the website: ATS parsers need a single column, real text, standard
  headings and no icons (`docs/research/recruiter-perspective.md`, section F). Two templates
  (`Public`, `Full`) in two languages from one data set. Rejected: HTML + headless Chromium (image
  size, font and hyphenation issues) and browser `window.print()` (no admin download, no variants).
- **Content lives in the database only.** The repository is public, so no CV data, phone number,
  address, photo or document is committed. The seed creates neutral example content so a fresh
  checkout renders. Real content is entered through the admin and backed up with the database.
- **Business statistics live in PostgreSQL; telemetry goes to Seq.** Application links record
  coarse events (opened, PDF downloaded, document downloaded) with timestamp and profile, no IP, no
  user agent. OpenTelemetry traces and logs go to the maintainer's Seq via OTLP for operations only.
  Rejected: reading statistics back from Seq (30-day retention, LAN-only, single-user licence, and it
  would couple a business feature to an ops tool).
- **Single admin, cookie auth, no 2FA, no OAuth.** One seeded admin sets the password on first
  login. Deny-by-default fallback policy, rate-limited login, security stamp in the cookie,
  Data Protection keys on a volume. The attack surface is one user editing texts; 2FA can follow if
  the threat model changes.
- **Application links are capability URLs, not logins.** 128-bit random token in the path, stored
  hashed, with an expiry and a revoke switch. No cookie, no session.
- **Public and admin API are separate OpenAPI documents and separate generated clients**, as in
  the maintainer's other projects, so admin code never reaches the public bundle.

## Design

- **Mockup A "Editorial" is the design**, with the fact bar from Mockup B: paper-coloured light
  theme, a serif for display and lede text, the system grotesk for UI and metadata, hairline rules,
  one accent colour, generous whitespace. Dark variant via `prefers-color-scheme` only; no toggle.
- **Never on this site**: gradient blobs, glassmorphism, Tailwind default palettes, scroll-triggered
  fade-ins, typewriter effects, "Hi, I'm … 👋", skill percentage bars, hero videos, parallax,
  cookie banners, contact forms, animated counters. The research lists most of these as negative
  signals; the rest are the visual clichés of the generic developer portfolio this site avoids.
- **Fonts are self-hosted** (Source Serif 4, OFL, Latin subsets as versioned `woff2`), never loaded
  from Google Fonts. The grotesk is the system stack. Monospace for code excerpts is the system
  stack.
- **Motion is limited to hover and focus states**; `prefers-reduced-motion` disables even those
  transitions.

## Process and repository

- **The repository is public, after the foundation.** Private while docs, skeleton, CI, gitleaks,
  branch protection and README are put in place; public before feature work starts, so the history
  shows the build from the first commit. Rejected: public from commit one (any slip is cached
  forever) and private until MVP (hides the process the repository is meant to show).
- **Commits are the maintainer's.** No generated attribution trailers. Small, imperative, with the
  why. Test and implementation in the same commit, named after the behaviour.
- **AI use is stated once, precisely, in the README.** Not on the site. Research is unambiguous:
  reviewers object to candidates who cannot explain what they shipped, not to the tooling.
- **`AGENTS.md` holds every rule; `CLAUDE.md` is a pointer.** Project-local agents are committed
  and contain no infrastructure facts.
- **Hosting is a VPS on the maintainer's own account**, not the chess club's server and not the
  homelab (which has no public IPv4). Staging runs in the homelab. The hosting provider and the
  server details are operational facts and live in private tooling, not here.
- **Two-space indentation everywhere**, one `.editorconfig`, one Prettier config. The maintainer's
  older Angular projects use four spaces; this repository does not inherit that.
- **Default branch `master` deploys production; `dev` deploys staging.** Branch protection on
  `master` once the repository is public.

## Phase 1 (2026-11-07)

- **The complete content schema ships in one migration**, including documents, interests and the
  application tables whose UI comes in phases 2 and 3. Later phases add UI, not migrations.
- **Child entities with prose get translation tables too** (fact labels, decisions, image captions,
  skill qualifiers, document labels). Rejected: `LabelDe`/`LabelEn` columns on the children, which
  would break the one rule for translations. The greeting of an application link keeps its two
  columns, because it is a single line on a single entity and never falls back.
- **Admin DTOs carry both locales side by side** (`de` required, `en` or null), and `PUT` replaces
  the whole resource including its ordered children. Rejected: per-locale endpoints and patch
  semantics; the editor saves one form, and replacing children in order is simpler than diffing.
- **The restricted Markdown renderer is written in-house** (`Shared/Application/Markdown/`) and
  runs on the server; public responses carry HTML, admin responses Markdown. Rejected: Markdig (a
  dependency for a subset one trusted author writes) and `marked` in the browser (public bundle
  size, sanitising on the client).
- **The Impressum takes the address from the profile** instead of repeating it in the Markdown body,
  so the address the law requires exists once.
- **Admin texts exist in German only** (`admin/admin-strings.ts`), kept out of the templates like
  every UI string. The admin has exactly one, German-speaking user; translating the admin would be
  work without a reader. The public site's chrome strings stay complete in both locales.
- **Deleting asks for confirmation inside the form**, not through `window.confirm`: testable, styled
  like the rest, and readable by screen readers in context.
