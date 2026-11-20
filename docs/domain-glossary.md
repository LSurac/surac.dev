# Domain glossary

The ubiquitous language. Code uses the English term; the German UI uses the German term. When a
term changes here, it changes in code, UI and docs together. Which role a term plays in which bounded
context is in `docs/domain.md`.

## Portfolio

| English (code) | German (UI) | Meaning |
|---|---|---|
| Profile | Profil | The person: name, title line, intro, working mode, contact, photo, fact bar. Exactly one. |
| Title line | Titelzeile | The search-term line under the name: "Fullstack-Entwickler · .NET & Angular". |
| Headline | Überschrift | The hero sentence ("Ich baue Software, die im Betrieb bestehen muss."). |
| Intro | Kurzprofil | Three to five sentences about what, with what, in which context. |
| Fact | Kennzahl | One cell of the fact bar: value plus label. |
| Experience | Berufsstation | A job with company, role, period, bullets, stack line. |
| Education | Ausbildung | School, apprenticeship, studies, with degree and grade. |
| Project | Projekt | A piece of work with a card on the start page and a case study page. |
| Featured (`Project.IsFeatured`) | Auf der Startseite | A project on the start page and in the CV; at most five visible. |
| Case study | Fallstudie | Problem, role, decisions with trade-offs, outcome, images, code excerpts. |
| Decision (project) | Entscheidung | One architectural choice in a project with its trade-off. |
| Skill group / Skill | Kenntnisbereich / Kenntnis | Grouped technologies with "since year", never with a level. |
| Interest | Interesse | One entry of the "Über mich" page (music, chess, homelab, family). |
| Interest kind (`InterestKind`) | Art | What an interest is about: music, chess, homelab, family, other. |
| Site section (`SiteSection`) | Abschnitt | One block of the start page: hero, facts, intro, projects, career, skills, working mode, contact. "Abschnitt" means nothing else. |
| Working mode / "agentic" | Arbeitsweise / agentisch | The start-page section on how the maintainer works with AI agents; its text is `WorkingMode` on the profile. |
| Translation | Übersetzung | The locale-specific prose of an entity; falls back to German. |
| Fallback field (`fallbackFields`) | nicht übersetzt | A field shown in German because the English one is empty. |
| Locale | Sprache | `de` or `en`; from the URL prefix. |

## Documents, CV and legal pages

| English (code) | German (UI) | Meaning |
|---|---|---|
| Document | Dokument | An uploaded file: photo, certificate, cover letter, image. Public images and link-only certificates are one concept. |
| Visibility | Sichtbarkeit | `Public` (open site) or `LinkOnly` (only via an application link). |
| CV variant | Lebenslauf-Variante | `Public` (no phone/address) or `Full`. Generated, not uploaded. |
| Legal page | Rechtstext | Impressum, Datenschutzerklärung. |

## Applying

| English (code) | German (UI) | Meaning |
|---|---|---|
| Application | Bewerbung | One application to one employer: company, position, status, notes. |
| Status (`ApplicationStatus`) | Status | Entwurf, Versendet, Antwort erhalten, Gespräch, Angebot, Absage, Zurückgezogen. |
| Applied on (`AppliedOn`) | Beworben am | The day the application went out; required for every status after `Draft`. |
| Application link | Bewerbungs-Link | The capability URL `/b/<token>` created for one application. |
| Audience (`ApplicationLink.Audience`, enum `AudienceProfile`) | Zielgruppe | `Public`, `Recruiter`, `TechLead`: decides the section order. Never told to the recipient. |
| Section order | Abschnittsreihenfolge | The ordered list of start-page sections for one audience. |
| Greeting (`GreetingDe`, `GreetingEn`) | Begrüßung | The one line a link shows above the start page. |
| Unlock (`UnlockFullPdf`, `AttachDocuments`) | freigeben | What a link adds to the open site: the full CV and documents. Never content. |
| Link documents (`ApplicationLink.Documents`) | Unterlagen | The link-only documents one link unlocks; a link carries only link-only documents. |
| Expiry (`ExpiresAt`) | Gültig bis / abgelaufen | The end of a link's life, 90 days by default; an expired link can be revived by a later date. |
| Revoke (`ApplicationLink.Revoke`) | widerrufen | End a link for good; it then answers like an unknown one. |
| Link event | Link-Ereignis | Opened, PDF downloaded, document downloaded; timestamp and locale only. |
| Opening (`LinkEventKind.Opened`) | Aufruf | One page view of `/b/<token>`; reloads, language switches and messenger previews count. |
| Sent, counted (`ApplicationPipeline.IsSent`) | versendet insgesamt | Every status except `Draft`; not the same as the status "Versendet". |
| Response rate | Antwortquote | Responded / sent in total; `Withdrawn` counts as sent without response. |
| Time to first open | Zeit bis zum ersten Aufruf | From the first link's creation to the first opening of any of the application's links. |
| Reminder (`ApplicationReminder`) | Erinnerung | The stateless weekly digest of applications `Sent` for 14 days or more. |

## Operation

| English (code) | German (UI) | Meaning |
|---|---|---|
| Admin | Admin | The single maintainer account. |
