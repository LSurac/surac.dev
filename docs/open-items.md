# Open items

What is still open. Only open lines stay here: a finished item is deleted, and what it taught goes
into the document it belongs to (`docs/decisions.md`, `docs/ops-and-deploy.md`, …).

## Acceptance on staging (maintainer)

The staging content was entered on 2026-11-08 and accepted by the maintainer the same day. Still
open in the admin:

- [ ] "Über mich": a chess photo, once one exists (Immich had only screenshots of games).

## Domain review (maintainer decides)

Questions from the first pass of `docs/domain.md` (section 7); each needs a yes or no before code
changes.

- [ ] Q1: keep `ApplicationLink.Profile` and only say "Zielgruppe" in `docs/domain-glossary.md`, or
  rename it to `Audience` (column rename migration, admin client break).
- [ ] Q2: reword "Bounded contexts are domain folders" in `docs/decisions.md` ("DDD-light") to
  point at the contexts in `docs/domain.md`; same for "domains" in `docs/architecture.md`.
- [ ] Q3: require `AppliedOn` for every status after `Draft` in `Application.Update`; today a `Sent`
  application without a date is never reminded (`ApplicationReminder.Due`).
- [ ] Q4: may an expired link be revived by a new `ExpiresAt` (`ApplicationLink.Update`), or is
  expiry final like revoking?
- [ ] Q5: one `Document` for public images and link-only certificates, or two concepts?
- [ ] Q6: the "Arbeitsweise" text lives in `SuracDev.Web/src/app/core/i18n/ui-strings.ts`, not in the
  admin; deliberate?
- [ ] Q7: keep the back-references in `DocumentService` (`IsShownPubliclyAsync`, `DeleteAsync`) as a
  deliberate exception, or let the foreign keys refuse the delete.
- [ ] Q8: should `DocumentService.UpdateAsync` refuse to make a document public while a link has it
  attached, so a link only ever carries link-only documents?
- [ ] Glossary drift: settle "Kenntnisbereich" vs. "Kenntnisgruppe" and "Interesse" vs. the admin's
  "Über mich"/"Abschnitt"/"Thema"; rename the admin's "Abschnitte" of interests
  (`admin-strings.ts`, `interests.list`) so "Abschnitt" means only a start-page section.
- [ ] Merge the reviewed terms from `docs/domain.md` section 2 into `docs/domain-glossary.md`.

## Engineering

- [ ] Rename `Domains/Pdf/` to `Domains/Cv/` (and the test folder); the code inside already says
  `Cv`. Namespace move only, no behaviour change.
- [ ] EF Core 11: `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0 is not on nuget.org yet (only
  11.0.0-rc.3 on 2026-11-19), so `Microsoft.EntityFrameworkCore`, `.Relational`, `.Design` and the
  Npgsql provider stay on 10.0.15 / 10.0.3 on net11.0 (comment in `SuracDev.Api.csproj`). Once 11.0.0
  is released: move all four together, run `ef-check` before and after (EF 11 stores the last
  migration id in the snapshot; expect a tooling-generated snapshot diff, never a hand edit), read the
  Npgsql 11 breaking changes (default PostgreSQL target 16; serialization failures and deadlocks in
  `SaveChanges` become `DbUpdateConcurrencyException`, which `GlobalExceptionHandler` does not map to
  409 today), then staging before production.
- [ ] TypeScript stays on ~6.0.3, not 7.0.2: `@angular/compiler-cli` and `@angular/build` 22.3 accept
  only `>=6.0 <6.1`, `typescript-eslint` 8.73 only `<6.1.0`. Revisit with the Angular major that
  supports TypeScript 7.
- [ ] Local test trap: the `docker info` probe in `SuracDev.Api.Tests/Shared/PostgresFixture.cs`
  (10 s timeout) reported Docker as down while it ran, skipped 104 integration tests and failed the
  local coverage gate at 54 %. `GITHUB_ACTIONS=true` forces them (331/331 green). Raise the timeout or
  add it to `docs/gotchas.md` next to the existing Docker entry.
- [ ] E2E noise: one run logged `[WebServer] Error [ERR_STREAM_UNABLE_TO_PIPE]: Cannot pipe to a
  closed or destroyed stream` from the SSR server after the application-link documents test (tests
  passed). Not reproduced in three reruns or on the old packages; likely an aborted download stream.
  Look at it if it shows up in CI.
