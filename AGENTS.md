# surac.dev — Working Rules

These rules apply to all work in this repository, by humans and by coding agents. `CLAUDE.md` only
points here. The documents under `docs/` are the source of truth for what they cover; this file says
how to work, not what exists.

## Where things live

Read the relevant document before touching related code, and update it (not this file) when
something changes. Do not duplicate their content here.

- `docs/architecture.md` — what exists: projects, domains, request flow, database conventions.
- `docs/decisions.md` — settled trade-offs that look like improvements and are not. Read before
  "fixing" something that looks wrong.
- `docs/content-model.md` — the entities, their translations, visibility rules and the application
  link / profile mechanism.
- `docs/domain-glossary.md` — the ubiquitous language (German/English) used in code and UI.
- `docs/design.md` — design tokens, typography, layout rules and the explicit list of things this
  site never does visually.
- `docs/testing.md` — the test pyramid, TDD rules, coverage thresholds, how tests are named.
- `docs/security-and-privacy.md` — authorization model, application links, personal data, what the
  public repository may never contain.
- `docs/ops-and-deploy.md` — hosting, CI/CD, staging vs production, backups, monitoring, telemetry.
- `docs/build-and-test.md` — the exact commands.
- `docs/gotchas.md` — non-obvious traps already hit once.
- `docs/seo.md` — search and link-preview setup, the SEO baseline, changes and their measurement dates.
- `docs/open-items.md` — what is still open: manual steps, production setup, the launch checks.
  Only open lines stay in it; a finished item is deleted, its lasting facts go to the document
  they belong to.
- `docs/research/` — the recruiter/hiring-manager research the information architecture is based on.

Infrastructure access facts (hosts, IPs, SSH targets, credentials) are **not** in this repository.
They live in the maintainer's private tooling. Workflows reference them through repository
variables and secrets only.

## Working agreement

- Preserve the architecture, framework choices, folder structure and public interfaces. No
  opportunistic refactoring while doing something else.
- No silent scope creep. If a change grows, say so and get a go-ahead.
- On ambiguity: stop and report. Do not make an architectural decision by guessing. If a spec or a
  request looks wrong, say so.
- Never touch without an explicit go: EF Core migrations, `compose.yaml`, `deploy.ps1`,
  `.github/workflows/`, `deploy/`, Dockerfiles, nginx config, `.env`, anything on a host.
- Generate EF Core migrations with the EF tooling, never by hand. Enums are stored as `int`, `0` is
  the fail-loud `Unknown` sentinel, and shipped values are frozen forever.
- New libraries may be *proposed* (problem solved, alternatives, cost in bundle size, maintenance
  and attack surface), never introduced autonomously. Approved: `Testcontainers.PostgreSql` and
  `coverlet` (tests only), `QuestPDF` (PDF export), `PdfPig` (tests only, reads the generated PDFs
  for the golden files), `@testing-library/angular` and `@playwright/test` (web tests only).
- Never put secrets, personal data or infrastructure facts into files, logs, commits or docs.
  `.env` is placed by hand on the host; `.env.example` carries empty keys. The seed contains neutral
  example content, never real CV data.

## Test-driven development

TDD is the working mode here, not a reporting line. The full rules are in `docs/testing.md`; the
short version:

- A behaviour change starts with a failing test. Red, green, refactor, in that order.
- Domain rules are tested as pure unit tests without a database. Endpoints are tested through the
  HTTP pipeline against a real PostgreSQL. UI components are tested as a user sees them (Testing
  Library queries by role and text, no implementation details). The critical flows have Playwright
  tests.
- Test and implementation go into the **same** commit. The commit message names the behaviour
  ("Reject expired application links"), never the activity ("add tests").
- Coverage thresholds (API 85 % lines, Web 80 % lines) are a CI gate. Do not lower them; do not
  game them with assertion-free tests.
- A test that is hard to write is a design signal. Report it instead of forcing it.

## Code style

- Readability over micro-performance. KISS for logic, not as an excuse to flatten the architecture.
  DRY only for real, harmful duplication. No needless interfaces, base classes, wrappers or `utils`
  dumping grounds.
- Guard clauses and early returns over deep nesting; three meaningful nesting levels are the limit.
  Small, focused functions and components with intention-revealing names.
- **2-space indentation everywhere** (C#, TypeScript, HTML, SCSS, JSON, YAML). One `.editorconfig`
  and one Prettier config; `printWidth` 110.
- C#: `sealed` classes, primary constructors, file-scoped namespaces, `TreatWarningsAsErrors`. DTOs
  are `sealed record`s in `Contracts/`. No AutoMapper; project in `Select`. Domain errors throw the
  shared `DomainException` hierarchy and are mapped centrally; no per-controller try/catch.
- XML documentation (C#): every public type has a `<summary>` saying what it is in the domain
  language and which rule it protects, not its name in other words. Every public method with
  behaviour (factories, behaviour methods, service use cases, controller actions) has one naming
  the effect and the rule. Properties, constructors and DTO members only where the name does not say it (null
  meaning, unit, invariant). One or two sentences, present tense, `<see cref>` for types,
  `<param>`/`<returns>` only when they add information; point to the document that holds a rule
  instead of repeating it. No summaries that restate the signature, none in tests (the test name
  is the documentation). `CS1591` stays suppressed: this is a review rule, not a compiler gate.
- Controllers are REST and documented for the OpenAPI documents: plural resource nouns, `GET`
  reads, `POST` creates and answers `201` with `Location`, `PUT` replaces, `DELETE` answers `204`,
  errors are ProblemDetails. Controller and action XML docs are deliberately detailed because they
  become the Swagger UI and the comments of the generated clients: a `<summary>`, `<remarks>` for
  rules and edge cases, `<param>` for every parameter and a `<response code="…">` for every status
  the action can answer, matched by `[ProducesResponseType]`.
- Angular: standalone components, Signals, `OnPush`, `strict` and `strictTemplates`, zoneless.
  Presentational components in `ui/` have inputs and outputs only and no injected services.
  Feature components in `public/` and `admin/` compose them and own the data loading. API access goes
  through the generated NSwag clients (`api/generated/`, never hand-edited) and thin service
  wrappers. Templates stay simple: no complex inline expressions, no nested conditional rendering.
- Code, identifiers, comments, XML docs, commit messages and documentation in **English**. UI texts
  exist in German and English and come from the content model or the i18n files, never hardcoded.
- Commits: small, focused, imperative, with the why. Backend, frontend and docs separated. No
  formatting-only changes mixed with logic. No generated attribution trailers.
- Reviews are structured as **"Muss geändert werden" / "Sollte geändert werden" / "Geschmack /
  optional"**.

## Architecture discipline

- Backend code is organised by domain (`Domains/<Name>/`). `Shared/` holds only genuinely
  cross-domain concerns. A new concept gets a domain folder, not a new layer.
- Entities protect their invariants: private setters, factory methods, behaviour methods that throw
  `DomainValidationException` or `DomainConflictException`. Application services orchestrate use
  cases and talk to EF Core directly. Value objects exist where they prevent a class of bugs
  (`Locale`, `Slug`), not everywhere.
- Keep filtering, sorting and paging in SQL. Make sure queries translate under Npgsql.
- Public and admin API surfaces are separate OpenAPI documents and separate generated clients, so
  admin code never lands in the public bundle.
- Preserve API contracts, routes, DTOs, config keys and database columns unless the task explicitly
  changes them.

## Execution

- Analyse references before deleting code.
- Validate before reporting done: `dotnet build`, `dotnet test`, `npm run lint`, `npm run test:ci`,
  `npm run build` (SSR build), and NSwag regeneration after API changes.
- Never filter build output with PowerShell `Select-Object -First N`; it kills the process and
  reports false success. Use `Select-String` or `grep`.
- Report changed files, gate results, removed dead code and remaining risks. Explanations to the
  maintainer are written in German.

## Agents

Project-local agents live in `.claude/agents/` and are committed on purpose: they are part of how
this repository is built.

- `explore` — read-only discovery: locate files, find usages, check imports, collect `file:line`
  facts.
- `ef-check` — read-only EF Core health: pending model changes, snapshot drift, missing
  `.Designer.cs`, migration consistency. Run before and after schema work.
- `implementer` — clearly specified implementation only: a bug with a known root cause, a refactor
  to an exact pattern, an endpoint or component from a precise spec, tests for a defined behaviour.

Architecture, data model, security, deployment and final judgement stay with the main agent and the
maintainer. Subagents collect facts or execute narrow tasks; they do not decide.
