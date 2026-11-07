# surac.dev

Personal developer profile of Leonard Surac: a server-side rendered Angular 22 frontend, a .NET 10
API, PostgreSQL, and a small admin area from which the CV, the projects and the application
links are maintained and the CV is exported as an ATS-friendly PDF.

The site exists for two readers at once: a recruiter who needs the facts and the PDF in under a
minute, and a hiring manager who wants to see how the person behind it actually builds software.
This repository is the second half of that answer. It is public on purpose.

## What this repository shows

- **The stack I work with in production**: ASP.NET Core 10, EF Core 10 on PostgreSQL, Angular 22
  with SSR, Signals and zoneless change detection, Docker Compose behind Caddy.
- **DDD-light vertical slices** in the API: each domain owns its entities, invariants, use cases and
  contracts. No repository layer over EF Core, no MediatR, no speculative abstractions. The reasoning
  is in `docs/decisions.md`.
- **Component-driven Angular**: a small design system of presentational components under `ui/`,
  composed by thin feature components under `public/` and `admin/`.
- **Test-driven development as the working mode**: every domain rule starts as a failing test,
  every endpoint has an integration test against a real PostgreSQL (Testcontainers), every UI
  component has a behaviour-level spec, and the critical flows have Playwright end-to-end tests.
  The CI gate enforces coverage thresholds. `docs/testing.md` describes the pyramid and the rules.
- **Operations as part of the product**: migrations at startup, health endpoint, backups with a
  restore drill, uptime monitoring, OpenTelemetry traces. `docs/ops-and-deploy.md`.

## How this was built

I work agentically. In practice that means:

- A written plan and a decision log come first (`docs/decisions.md`); the rules for every change
  are in `AGENTS.md`.
- AI coding agents (Claude Code) work inside those rules: research, scaffolding, boilerplate,
  first drafts of tests.
- Architecture, data model, security, deployment, every review and every commit are mine.
- Small commits that name the behaviour they add, tests before implementation, documentation that
  explains why.

## Repository layout

| Path | Contents |
|---|---|
| `SuracDev.Api/` | ASP.NET Core API. `Domains/<Name>/{Domain,Application,Contracts,Controllers}`, `Shared/`, `Data/` |
| `SuracDev.Api.Tests/` | xUnit: domain unit tests, API integration tests on PostgreSQL via Testcontainers |
| `SuracDev.Web/` | Angular 22 SSR app. `src/app/{core,ui,public,admin,layout,api/generated}`, Playwright E2E under `e2e/` |
| `deploy/` | Caddyfile, backup and healthwatch scripts, systemd units for the production host |
| `docs/` | Architecture, decisions, content model, design, testing, security, operations, roadmap |
| `.github/workflows/` | CI gates, production and staging deploys, monthly dependency audit |

## Running it locally

Prerequisites: .NET 10 SDK, Node 26, Docker (for PostgreSQL and the integration tests).

```powershell
# API (expects ConnectionStrings__Default; see docs/build-and-test.md for a local Postgres)
dotnet run --project SuracDev.Api

# Web (dev server with proxy to the API on port 5180)
cd SuracDev.Web
npm ci
npm start
```

The full gate, which CI runs on every push:

```powershell
dotnet build SuracDev.slnx
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj --no-build
cd SuracDev.Web; npm run lint; npm run test:ci; npm run build
```

## Content and licence

The code is MIT licensed (see `LICENSE`). The content of the site (texts, CV data, photos,
documents) is not part of the licence and not part of this repository: it lives in the database and
is maintained through the admin area. The repository contains only schema, migrations and a neutral
example seed.

## Status

Foundation. See `docs/roadmap.md` for the phases and `docs/open-items.md` for what is in flight.
