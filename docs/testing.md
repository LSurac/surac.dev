# Testing

Test-driven development is the working mode of this repository. This document is the contract:
what kinds of tests exist, what each is for, how they are named, and what the CI gate enforces.

## The pyramid

| Level | Tool | Scope | Database | Speed |
|---|---|---|---|---|
| Domain unit | xUnit | entities, value objects, audience section order, PDF document model | none | ms |
| API integration | xUnit + `WebApplicationFactory` + Testcontainers | one HTTP request through the real pipeline: routing, auth, validation, EF, ProblemDetails | real PostgreSQL 17 per test (template copy) | 10–100 ms per test after container start |
| UI component | Vitest + Angular Testing Library | one `ui/` or feature component as a user sees it | none, API mocked at the client boundary | ms |
| End-to-end | Playwright | the four critical flows against the running stack | throwaway PostgreSQL | seconds |

The four end-to-end flows: (1) the start page renders server-side with the profile and a working
CV download, (2) the admin logs in, edits a text, and the public page shows it, (3) an application
link opens with its profile and greeting and unlocks its documents, (4) an expired or revoked link
answers 404 in both locales.

## Rules

1. **Red first.** A behaviour starts as a failing test that describes it. The implementation makes
   it pass; refactoring keeps it green. Reviewers may ask for the red state in a demonstration.
2. **One behaviour per test, named as a sentence.** `Login_sets_the_password_on_first_attempt`,
   `Expired_links_answer_not_found`, `renders the fact bar with four cells`. The method name is the
   documentation; no comments restating it.
3. **Test and implementation in the same commit**, message names the behaviour. Red-only commits
   are not pushed; they look staged.
4. **Domain tests are pure.** No `DbContext`, no `HttpClient`, no clock. Time comes in through a
   parameter or an injected `TimeProvider`.
5. **Integration tests hit the real thing.** Real PostgreSQL via Testcontainers (migrations run on
   a template database once per run; every test gets a copy), real cookie auth, real
   ProblemDetails. No `InMemory` provider: it hides translation and constraint errors.
6. **Authorization is tested generically.** One test enumerates every endpoint via `ApiExplorer`
   and asserts that anonymous requests get 401 unless the endpoint is on the explicit allow list.
   A new endpoint fails closed and shows up in the diff of that list.
7. **UI tests query like a user.** `getByRole`, `getByText`, `getByLabelText`. No component
   internals, no `fixture.debugElement.query(By.css(...))` for behaviour. Presentational components
   are tested with inputs and emitted outputs; feature components with a mocked generated client.
8. **Coverage is a floor, not a target.** API 85 % lines, Web 80 % lines, enforced in CI. Excluded
   by configuration: migrations, generated clients, `Program.cs` wiring that integration tests
   exercise anyway, the dev-only styleguide route.
9. **No sleeps, no retries in unit or integration tests.** Flakiness is a bug.
10. **A test that is hard to write is a design finding.** Report it; do not mock around it.

## Layout and naming

```
SuracDev.Api.Tests/
  Shared/
    PostgresFixture.cs          Testcontainers lifecycle, template database, per-test copies
    ApiFactory.cs               WebApplicationFactory bound to a test database
    ApiClient.cs                helpers: login as admin, call with/without cookie
  Domains/<Name>/
    <Entity>Tests.cs            domain unit tests
    <Controller>EndpointTests.cs integration tests
  Shared/AuthorizationSweepTests.cs
SuracDev.Web/
  src/app/**/<component>.spec.ts   next to the component
  e2e/<flow>.e2e.ts                Playwright
```

Integration test classes carry `[Collection(PostgresCollection.Name)]` and use
`[PostgresFact]`, which skips on a workstation without Docker and never in CI.

## Commands

```powershell
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj -p:CollectCoverage=true -p:Threshold=85 -p:ThresholdType=line -p:ThresholdStat=total

cd SuracDev.Web
npm run test          # watch
npm run test:ci       # once, with coverage and threshold
npm run e2e           # Playwright, needs Docker for the throwaway database
```

## What CI enforces

`gates.yml` runs on every push and pull request: API build and tests with the coverage threshold,
Web lint, Web tests with the coverage threshold, Web SSR build, NSwag contract check (generated
clients must match the API), `gitleaks`. The E2E suite runs in the deploy workflows before anything
reaches a host. A red gate blocks the deploy.
