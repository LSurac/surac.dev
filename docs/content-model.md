# Content model

The entities the admin maintains, their translations, visibility rules and the application-link
mechanism. Names here are the ubiquitous language (`docs/domain-glossary.md`). The schema is built
in phase 1 as a whole so that later phases add UI, not migrations.

## Localisation

- Supported locales: `de` (primary) and `en`. `Locale` is a value object wrapping the two-letter code.
- Every entity with prose has a translation table. Structural data (dates, order, links, visibility,
  enum values) lives once on the entity.
- Reading: the API resolves a requested locale and falls back field by field to `de`. The response
  marks fallback fields so the admin can show "untranslated" and the public site can decide to hide
  a section that is not translated yet.
- Writing: the admin edits both locales side by side. Saving German is mandatory, English optional.
  An English set whose fields are all empty is removed, so the reader falls back to German.
- Children with prose (fact labels, decisions, image captions, skill qualifiers, document labels)
  have their own translation tables as well; a fallback in a child marks the parent field
  (`facts`, `decisions`) in `fallbackFields`.

## Entities

### Profile (singleton)

The person. Exactly one row.

| Field | Notes |
|---|---|
| `FullName`, `Email`, `Phone`, `Location` | Phone and street address are `Full`-only (see visibility). |
| `AddressLines` | Only for the full PDF and the Impressum. |
| `GitHubUrl`, `LinkedInUrl` | |
| `PhotoDocumentId` | Reference to an uploaded public image document. |
| `Translations`: `TitleLine`, `Headline`, `Intro`, `AvailabilityLine` | `TitleLine` is the search-term line ("Fullstack-Entwickler · .NET & Angular"). |
| `Facts` (ordered, up to 4) | The fact bar: `Value` ("5", "1,8", "C1") plus translated `Label`. |

### Experience

A career station. Ordered by `StartDate` descending.

`Company`, `Location`, `StartDate` (DateOnly), `EndDate` (nullable = current), `SortOrder`,
`IsVisible`, translations: `RoleTitle`, `Summary`, `Bullets` (ordered list of short strings),
`StackLine`.

### Education

`Institution`, `Location`, `StartDate`, `EndDate`, `Grade` (free text, e.g. "1,8"), `SortOrder`,
`IsVisible`, translations: `Degree`, `Summary`.

### Project

A project card plus its case study.

| Field | Notes |
|---|---|
| `Slug` (value object, unique, per locale route) | `projekte/<slug>` and `en/projects/<slug>`; the slug is shared across locales. |
| `Status` enum | `Unknown=0, Live=1, InProduction=2, Archived=3, Planned=4` |
| `StartedYear`, `EndedYear` (nullable) | |
| `LiveUrl`, `RepositoryUrl` (nullable) | |
| `StackTags` (ordered list of strings) | Rendered as a stack line, never as bars. |
| `SortOrder`, `IsFeatured`, `IsVisible` | Featured projects appear on the start page (max 5). |
| Translations: `Title`, `Teaser`, `Problem`, `MyRole`, `Outcome`, `CaseStudyMarkdown` | The case study is Markdown with a restricted renderer (headings, lists, links, images from documents, fenced code). |
| `Decisions` (ordered, translated): `Title`, `TradeOff` | Two or three per project. |
| `Images` (ordered): `DocumentId`, translated `Caption` | |

### SkillGroup and Skill

`SkillGroup`: `SortOrder`, translated `Name` (Backend, Frontend, Betrieb, Arbeitsweise).
`Skill`: `GroupId`, `Name` (not translated: technology names), `SinceYear` (nullable),
`Qualifier` (translated, optional: "Legacy", "projektspezifisch"), `SortOrder`. No level, no
percentage, ever.

### Interest

Content of the "Über mich" page. `Kind` enum `Unknown=0, Music=1, Chess=2, Homelab=3, Family=4,
Other=5`, `SortOrder`, `IsVisible`, `ImageDocumentId` (nullable), translations: `Title`, `Body`
(Markdown).

### Document

An uploaded file. `FileName`, `ContentType`, `SizeBytes`, `StoragePath` (outside the web root,
random name), `Sha256`, `Kind` enum `Unknown=0, Photo=1, Certificate=2, CoverLetter=3, Image=4,
Other=5`, `Visibility` enum `Unknown=0, Public=1, LinkOnly=2`, `UploadedAt`, translated `Label`.
Allowed types: PDF, PNG, JPEG, WebP. Size limit 10 MB. Generated CV PDFs are **not** documents;
they are rendered on demand (docs/decisions.md explains why without a cache).

### Application and ApplicationLink

`Application` is the maintainer's mini ATS: `Company`, `Position`, `JobAdUrl`, `AppliedOn`
(DateOnly, nullable), `Status` enum `Unknown=0, Draft=1, Sent=2, Replied=3, Interview=4, Offer=5,
Rejected=6, Withdrawn=7`, `Notes` (Markdown), `CreatedAt`, `UpdatedAt`.

`ApplicationLink` belongs to exactly one application:

| Field | Notes |
|---|---|
| `TokenHash` | Public path `/b/<token>`; the token is 128 random bits in base64url, generated server-side; only its SHA-256 (hex) is stored. The clear token is shown once at creation. |
| `Profile` enum | `Unknown=0, Public=1, Recruiter=2, TechLead=3` |
| `GreetingDe`, `GreetingEn` | Optional one-line greeting ("Für das Team Sales Backend bei …"). |
| `ExpiresAt` | Required; default 90 days. |
| `IsRevoked` | Manual kill switch. |
| `UnlockFullPdf` | Full PDF with phone and address. |
| `Documents` | The certificates / cover letter unlocked by this link (`LinkOnly` documents only). |

`LinkEvent`: `LinkId`, `Kind` enum `Unknown=0, Opened=1, PdfDownloaded=2, DocumentDownloaded=3`,
`OccurredAt`, `Locale`, `DocumentId` (nullable). **No IP address, no user agent, no referrer.**
The statistics page aggregates these per application.

Profile behaviour is configuration, not code paths: a `SectionOrder` per profile (list of section
keys) lives in `Shared/Application/Audience/` and is covered by tests. `Public` is the order the open
site uses.

### LegalPage

`Key` enum `Unknown=0, Imprint=1, Privacy=2`, translations: `Title`, `BodyMarkdown`, `UpdatedAt`.
`UpdatedAt` is per locale and moves only when that locale's text actually changes. The public
Impressum response adds name, postal address and e-mail from the profile, so the address is
maintained in one place.

## Visibility rules

| Data | Open site | Application link | Admin |
|---|---|---|---|
| Name, e-mail, location, GitHub, LinkedIn | yes | yes | yes |
| Phone, street address | no (Impressum shows the address as required by law) | in the full PDF when `UnlockFullPdf` | yes |
| CV PDF `Public` (no phone/address) | yes | yes | yes |
| CV PDF `Full` | no | when `UnlockFullPdf` | yes |
| Documents `Public` | yes | yes | yes |
| Documents `LinkOnly` | no | only those attached to the link | yes |
| Applications, link statistics | no | no | yes |

## Seed

The seeder runs after migrations and only fills empty tables: one admin user without a password,
one profile with neutral example content ("Max Mustermann"-style, clearly marked as example) and a
full fact bar, one career station, one education entry, one featured example project, two skill
groups, the two legal pages with placeholder text, all in both locales. It never contains the
maintainer's real data.
