# surac.dev — Domain

Status: first pass by the agent (2026-11-19), derived from code, tests and the existing docs; every
decision names its source, and what the agent decided alone is marked "agent, unreviewed". The
maintainer decided the questions of section 7 on 2026-11-20; the terms of section 2 are merged into
`docs/domain-glossary.md`.

This file is the domain view: the story, the contexts cut by behaviour, how they depend on each other
and which role one concept plays in each. It does not repeat what other documents hold: the
vocabulary is `docs/domain-glossary.md`, the entities and visibility rules are
`docs/content-model.md`, the code layout is `docs/architecture.md`, the trade-offs are
`docs/decisions.md`.

## 1. Domain story (as-is, coarse)

The domain experts are the maintainer (German, writes the content, applies for jobs) and the readers
he writes for: recruiters and HR (German, decide in under a minute), tech leads and hiring managers
(read projects and code) and the applicant tracking systems that parse the CV
(`docs/research/recruiter-perspective.md`). The experts' words are German; the story uses the English
glossary terms with the German word where the UI uses it.

| # | Actor | Activity | Work object |
|---|---|---|---|
| 1 | Maintainer | writes, in German and optionally English | profile (Profil), career stations (Werdegang), education, skills (Kenntnisse), projects with case studies |
| 2 | Maintainer | uploads and sets the visibility of | documents: photo, images, certificates (Zeugnisse), cover letters (Anschreiben) |
| 3 | Maintainer | writes | legal pages (Impressum, Datenschutzerklärung) |
| 4 | Visitor | opens | the open site: start page, case studies, "Über mich" |
| 5 | Visitor | downloads | the public CV (Lebenslauf, without phone and address) |
| 6 | Maintainer | records | an application (Bewerbung) to one company for one position |
| 7 | Maintainer | creates for the application | an application link (Bewerbungs-Link) with audience (Zielgruppe), greeting, expiry, unlocked CV and documents (Unterlagen) |
| 8 | Maintainer | sends, outside the system | the link with the application |
| 9 | Recipient (recruiter or tech lead) | opens | the link: the start page in the audience's section order, with the greeting |
| 10 | Recipient | downloads | the full CV and the unlocked documents |
| 11 | System | records | a link event (opening, CV download, document download) without visitor data |
| 12 | Employer's ATS | parses | the CV as text |
| 13 | Maintainer | moves | the application's status (sent, reply, interview, offer, rejection, withdrawn) |
| 14 | System | sends each Monday | a reminder digest of applications without reply for 14 days (ntfy) |
| 15 | Maintainer | reads | the statistics: response rate (Antwortquote), time to first opening |
| 16 | Maintainer | revokes, or lets expire | an application link |

## 2. Ubiquitous language

The glossary is `docs/domain-glossary.md`. The first pass found sixteen terms it lacked (Aufruf,
Unterlagen, freigeben, widerrufen, Gültig bis, Begrüßung, Abschnitt, Auf der Startseite, Status,
versendet insgesamt, Antwortquote, Zeit bis zum ersten Aufruf, Erinnerung, Beworben am, the kind of an
interest, nicht übersetzt); they were merged into it on 2026-11-20. This section keeps the places
where glossary, code and UI disagreed and how each was settled.

### Conflicts

| Term | Where | Conflict | Settled (2026-11-20) |
|---|---|---|---|
| Profile | `Profile` (the person) and `ApplicationLink.Profile` / `AudienceProfile` | One English word for two things in one API (check 3). The German UI already separated them: "Profil" and "Zielgruppe". | Renamed to `ApplicationLink.Audience` (column rename migration, admin field `audience`); glossary "Zielgruppe" (Q1). |
| Abschnitt | `SiteSection` and the admin list of interests ("Abschnitte", `admin-strings.ts`, `interests.list`) | One German word for a start-page block and an "Über mich" entry. | The admin calls the entries "Interessen"; "Abschnitt" means only a start-page section. |
| Kenntnisbereich / Kenntnisgruppe | glossary vs. admin UI (`skills.list`) | Two German words for `SkillGroup`. | "Kenntnisbereich" everywhere. |
| Interesse | glossary vs. admin UI | The UI never said "Interesse"; it said "Über mich", "Abschnitt", "Thema". | The UI follows the glossary: "Interesse"; the kind of an interest is its "Art". |
| Versendet | `ApplicationStatus.Sent` vs. `ApplicationPipeline.IsSent` | The status "Versendet" is one value; "sent" in the statistics is every status after `Draft`. | The glossary names the statistic "versendet insgesamt"; no code change. |
| Pdf | `Domains/Pdf/` | The folder names the file format, not the domain object; the code inside says `CvService`, `CvModel`, `CvDocument`. | Open: `Domains/Cv/`, engineering item in `docs/open-items.md`. |
| Company | `Experience.Company`, `Application.Company` | Same word, two roles (former employer, prospective employer). | Fine: different contexts (section 5). |

## 3. Bounded contexts

| Context | Code | Type | What happens there | Persistence |
|---|---|---|---|---|
| Presenting the portfolio | `Domains/Profiles`, `Career`, `Projects`, `Skills`, `Interests`; web `public/home`, `case-study`, `about` and their admin editors | core | the maintainer writes and translates; the visitor reads, in the HR-first order | state (edit in place, translation tables) |
| Applying and following up | `Domains/Applications`, `Shared/Application/Audience`; web `public/application-link`, `admin/applications`, `admin/statistics` | core | record an application, create, change, revoke and resolve links, count openings and downloads, remind, compute the response rate | state (`Application`, `ApplicationLink`) plus an append-only ledger (`LinkEvent`) |
| Rendering the CV | `Domains/Pdf` | supporting | render the public or full CV in one locale, two pages, for parsers and printers | none: rendered on every request (`docs/decisions.md`, Phase 1) |
| Filing documents | `Domains/Documents` | supporting | upload, sniff, label, decide who may download (public or link-only), deliver, delete | state plus files on a volume |
| Publishing the legal pages | `Domains/Legal` | generic | write Impressum and privacy policy per locale, stamp their last change | state |
| Signing in the maintainer | `Domains/Identity`, `Shared/Infrastructure/SecurityStampValidator.cs` | generic | first password, login, password change, invalidating sessions | state |
| Observing use and operation | `Shared/Infrastructure/Telemetry`, `Domains/Health` | generic | log requests, page views and logins, export traces and metrics, answer health checks | none in the database: exported to Seq (`docs/ops-and-deploy.md`, "Telemetry") |

Shared kernel on purpose: `Shared/Application` (`Locale`, translation fallback, the restricted
Markdown renderer, `Require`, the `DomainException` hierarchy) and the one `SuracDevDbContext`
(`docs/decisions.md`, "DDD-light").

Why the cuts are where they are (source: agent, unreviewed, from the evidence named):

- **The five content folders are one context, not five.** Profile, career, projects, skills and
  interests have the same actors (the maintainer writes, the visitor reads), the same lifecycle
  (edit in place, German required, English optional, field fallback) and the same rules (visibility,
  "since year, never a level", HR-first order). The folders are modules cut by work object, which is
  check 1's red flag for a context but fine for a module. `docs/decisions.md` ("DDD-light") and
  `docs/architecture.md` ("Backend layout") say so since 2026-11-20 (Q2).
- **Applying is its own context.** Different actor (the recipient, anonymous but holding a
  capability), different lifecycle (links expire and are revoked, events only grow), different legal
  role (`Notes` and the statistics never leave the admin, `docs/security-and-privacy.md`). It stays
  one context with two entry points (`LinkAccessService` for the recipient, the admin services for
  the maintainer): the ledger the recipient writes is read by the statistics in the same context,
  and both work on the same link.
- **Rendering the CV is separate from presenting.** Different reader (the ATS parser), different
  rules (one column, standard headings, two pages, bundled Lato; `docs/decisions.md`, Phase 1), and
  the cut already exists in the code. It owns no data.
- **Filing documents is separate.** Different lifecycle (immutable content, upload and delete),
  different legal role (certificates and cover letters carry third-party data and are never public,
  `Document.ChangeVisibility`), different storage (files on a volume).
- **Not a context**: the working-mode section ("Arbeitsweise"). It is a start-page section of
  Presenting the portfolio; since 2026-11-20 its text is `WorkingMode` on the profile, edited in the
  admin like the intro (Q6).

## 4. Context map

```mermaid
flowchart LR
  Identity[Signing in the maintainer]
  Documents[Filing documents]
  Portfolio[Presenting the portfolio]
  Cv[Rendering the CV]
  Legal[Publishing the legal pages]
  Applying[Applying and following up]
  Observing[Observing use and operation]
  Ntfy[(ntfy)]
  Seq[(Seq)]
  Web[Angular site and admin]
  Ats[Employer's ATS]

  Documents -->|public image ids| Portfolio
  Documents -->|link-only documents| Applying
  Portfolio -->|conformist: public read models, private contact| Cv
  Portfolio -->|conformist: name, address, e-mail| Legal
  Cv -->|public or full CV| Applying
  Ntfy -->|conformist: publish API| Applying
  Seq -->|published language: OTLP| Observing
  Identity -->|cookie, security stamp| Web
  Portfolio -->|OHS: public and admin OpenAPI| Web
  Applying -->|OHS: link, section order| Web
  Cv -->|PDF| Ats
```

Arrows point downstream. ntfy and Seq are upstream because their protocol wins, although the data
flows towards them.

| Upstream | Downstream | Pattern | Content |
|---|---|---|---|
| Filing documents | Presenting the portfolio | customer / supplier | document ids that must name public images (`DocumentReferences.EnsurePublicImagesAsync`) |
| Filing documents | Applying | customer / supplier | link-only documents to attach (`ApplicationLink.AttachDocuments`) and to deliver (`DocumentService.OpenAttachedAsync`) |
| Presenting the portfolio | Rendering the CV | conformist | the public read models of profile, career, skills and featured projects, plus phone and address read from `Profile` (`CvService.cs`) |
| Presenting the portfolio | Publishing the legal pages | conformist | name, postal address and e-mail for the Impressum (`LegalPageService.cs`) |
| Rendering the CV | Applying | customer / supplier | `RenderPublicAsync` / `RenderFullAsync`, chosen by `UnlockFullPdf` |
| ntfy | Applying | conformist | one HTTP publish per digest (`ApplicationReminderService.cs`) |
| Seq | Observing use and operation | published language | OTLP traces, logs, metrics and the usage events |
| Signing in the maintainer | every admin endpoint | generic upstream | the `suracdev.auth` cookie, deny by default (`docs/security-and-privacy.md`) |
| Presenting the portfolio, Applying, Rendering the CV, Filing documents, Legal | Angular site and admin | open host service + published language | two OpenAPI documents, two generated clients (`docs/decisions.md`, "Architecture") |
| Applying | Angular start page | shared kernel | `SiteSection` and the section order: `SectionOrder.cs` in the API, `PUBLIC_SECTIONS` mirrored in `SuracDev.Web/src/app/public/home/home-page.ts` |

Allowed dependencies are exactly these arrows, plus the shared kernel. Two arrows run backwards in
today's code; section 6 lists them.

## 5. One concept, several roles

| Concept | Context | Role | Is |
|---|---|---|---|
| The maintainer | Presenting the portfolio | the person presented | `Profile` |
| | Rendering the CV | the CV's subject | `CvModel` |
| | Applying | the applicant | implicit: every `Application` is his |
| | Publishing the legal pages | the service provider named in the Impressum | name and address taken from `Profile` |
| | Signing in the maintainer | the admin | `AdminUser` |
| A reader | Presenting the portfolio | visitor (anonymous) | no entity |
| | Applying | recipient of one link, filed under an audience | `ApplicationLink.Audience` (never told to the recipient) |
| | Rendering the CV | ATS parser or printer | no entity |
| | Observing use and operation | a page view with a shortened address | usage event in Seq |
| A company | Presenting the portfolio | former employer | `Experience.Company` |
| | Applying | prospective employer | `Application.Company` |
| An uploaded file | Filing documents | document (Dokument) | `Document` |
| | Presenting the portfolio | photo, case-study image, interest image | `Profile.PhotoDocumentId`, `ProjectImage`, `Interest.ImageDocumentId` |
| | Applying | Unterlagen; a downloaded document in the ledger | `ApplicationLinkDocument`, `LinkEvent.DocumentId` |

## 6. Criteria check

| # | Check | Result |
|---|---|---|
| 1 | Cut by behaviour, not by data | failed first: the code called its five content folders contexts. Fixed by one context "Presenting the portfolio"; the folders are modules (Q2, decided). |
| 2 | The name says what happens there | `Domains/Pdf` names a file format; proposal `Cv` (open item). `Identity` promises more than one admin login; kept as the generic name. |
| 3 | A word means one thing inside a context | failed first: "Profile" (person / audience) and "Abschnitt" (start-page block / interest entry). Fixed on 2026-11-20 (`ApplicationLink.Audience`, "Interessen" in the admin); section 2. |
| 4 | One concept in several contexts has a role name in each | ok: section 5. The UI already says "Unterlagen" for documents in the application context. |
| 5 | Names use the experts' verbs | ok, apart from the drifts in section 2. |
| 6 | A name promises only what exists | ok. `UnlockFullPdf` unlocks exactly the full CV. |
| 7 | Odd terms checked against published language | kept: ATS, Impressum, Datenschutzerklärung, capability URL. |
| 8 | One expert word for two lifecycles is asked about, not split | asked: one `Document` serves public site images and link-only certificates. Stays one concept (Q5, decided). |
| 9 | The cut has evidence | ok: section 3 names it per boundary. |
| 10 | Each context has a type and a persistence style with a reason | ok: two core, two supporting, three generic; one ledger (`LinkEvent`), nothing event-sourced, because no decision depends on the history. |
| 11 | No cycle, no shared model by accident | exception: `DocumentService` reads the tables of the contexts it supplies (`Profiles`, `Projects`, `Interests` in `IsShownPubliclyAsync`; `ApplicationLinks` in `UpdateAsync`, `DeleteAsync` and `OpenAttachedAsync`), so Filing documents and its two customers know each other. The shared `SuracDevDbContext` is deliberate (`docs/decisions.md`, "DDD-light"); the back-reference is a deliberate exception (Q7, decided). |
| 12 | First-pass names re-checked after the cut changed | ok. |

All other checks passed.

## 7. Decisions

| # | Question | Decision | Source |
|---|---|---|---|
| D1 | Which language does the domain story use? | English with the German UI words, like the other docs | `AGENTS.md`, "Code style" |
| D2 | Is the start page's working-mode text content? | ~~treated as UI text, as built~~ content of the profile since 2026-11-20 (Q6) | maintainer, 2026-11-20 |
| D3 | Recipient access and the maintainer's tracking: one context or two? | one context, two entry points | agent, unreviewed (section 3) |
| D4 | Is the CV its own context? | yes, supporting, without data | `Domains/Pdf`, `docs/decisions.md` (Phase 1) |
| D5 | Persistence of link events | append-only ledger, not event-sourced | `LinkEvent.cs`, `docs/decisions.md` (Architecture) |
| D6 | Which contexts are core? | presenting the portfolio and applying | agent, unreviewed |
| D7 | Shared `DbContext` and `Shared/Application` | shared kernel on purpose | `docs/decisions.md`, "DDD-light" |

### Questions of the first pass (decided 2026-11-20)

The maintainer answered all of them on 2026-11-20; the reasons are in `docs/decisions.md` ("Domain
review").

- **Q1 — "Profile" twice.** `ApplicationLink.Profile` (column, admin DTO field `profile`) held an
  `AudienceProfile`, next to the `Profile` entity. Options: (a) accept and only fix the glossary;
  (b) rename the property and DTO field to `Audience` (an EF migration renaming a column, a breaking
  change of the admin client). **Decided: (b)**, `ApplicationLink.Audience`, migration
  `RenameLinkProfileToAudience`.
- **Q2 — What is a bounded context here?** `docs/decisions.md` said "Bounded contexts are domain
  folders"; this file cuts by behaviour and finds seven contexts over eleven folders. **Decided:**
  "Domain folders are modules; the bounded contexts are in `docs/domain.md`."
- **Q3 — Sent without a date.** `Application.Update` accepted `Sent` (and every later status) with
  `AppliedOn` empty, and `ApplicationReminder.Due` silently skipped such an application. **Decided:**
  every status after `Draft` requires `AppliedOn` (400, keyed `appliedOn`). Existing rows are not
  migrated; the rule applies on the next save. Production had no such row on 2026-11-20.
- **Q4 — Reviving an expired link.** Revoking is final, but an expired link can be brought back by
  moving `ExpiresAt` into the future (`ApplicationLink.Update`). **Decided:** reviving stays allowed;
  expiry is a timer, revoking is the way to close a link for good.
- **Q5 — One `Document`, two lifecycles.** A public image and a link-only certificate share one entity,
  one table and one admin list. **Decided:** one concept; the visibility rules keep them apart.
- **Q6 — Working mode as content.** The "Arbeitsweise" text was UI text in the web bundle.
  **Decided:** it is content: `WorkingMode` on the profile translations, edited in the admin; the
  migration `AddProfileWorkingMode` carried the former text over, so the page did not change.
- **Q7 — Back-references from documents.** `DocumentService` checks the profile, projects, interests
  and links before a visibility change or delete. **Decided:** kept as a deliberate exception for the
  precise messages.
- **Q8 — Document attached, then made public.** `DocumentService.UpdateAsync` let an attached document
  become public afterwards. **Decided:** "a link only carries link-only documents" is an invariant;
  making an attached document public answers 409.
