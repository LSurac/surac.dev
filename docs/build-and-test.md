# Build and test

Exact commands. Run from the repository root unless stated otherwise.

## Prerequisites

- .NET 10 SDK (10.0.3xx)
- Node 26, npm 10
- Docker Desktop (integration tests, local PostgreSQL, E2E)
- `gitleaks` on the PATH for the pre-commit hook (`winget install gitleaks` or
  `scoop install gitleaks`); CI runs it regardless.

## Local database

```powershell
docker run -d --name surac-dev-postgres -e POSTGRES_USER=suracdev -e POSTGRES_PASSWORD=suracdev -e POSTGRES_DB=suracdev -p 5432:5432 postgres:17-alpine
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=suracdev;Username=suracdev;Password=suracdev"
```

The same values are the design-time defaults of `SuracDevDbContextFactory`, so `dotnet ef`
works without further configuration against this container.

## API

```powershell
dotnet build SuracDev.slnx
dotnet run --project SuracDev.Api            # http://localhost:5180, Swagger UI at /swagger in Development
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj --no-build
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj -p:CollectCoverage=true -p:Threshold=85 -p:ThresholdType=line -p:ThresholdStat=total
```

Migrations (only via tooling, only with an explicit go):

```powershell
dotnet ef migrations add <Name> --project SuracDev.Api --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project SuracDev.Api
```

## Web

```powershell
cd SuracDev.Web
npm ci
npm start                 # dev server on http://localhost:4200 with /api proxied to :5180
npm run lint
npm run test              # Vitest watch
npm run test:ci           # once, coverage, threshold 80 % lines
npm run build             # production SSR build into dist/surac-dev/{browser,server}
npm run serve:ssr         # node dist/surac-dev/server/server.mjs on :4000
npm run api:generate      # regenerate NSwag clients (starts the API in the OpenApi environment)
npm run contract:check    # fails if the committed clients are stale
npm run e2e               # Playwright against a throwaway PostgreSQL (needs Docker)
```

## The gate

What must be green before "done", and what `gates.yml` runs:

```powershell
dotnet build SuracDev.slnx
dotnet test SuracDev.Api.Tests/SuracDev.Api.Tests.csproj --no-build -p:CollectCoverage=true -p:Threshold=85 -p:ThresholdType=line -p:ThresholdStat=total
cd SuracDev.Web
npm run lint
npm run test:ci
npm run build
npm run contract:check
```

## Pre-commit hook

```powershell
git config core.hooksPath .githooks
```

`.githooks/pre-commit` runs `gitleaks protect --staged --config .gitleaks.toml` and refuses the
commit on a finding. It is opt-in per clone because hooks are not versioned by git itself.

## Deploy

See `docs/ops-and-deploy.md`. Never deploy from a dirty tree; `deploy.ps1 -Production` refuses to.
