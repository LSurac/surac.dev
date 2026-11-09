# Architecture

What exists and how it fits together. For why, see `docs/decisions.md`; for the data, see
`docs/content-model.md`.

## Projects

| Project | Role |
|---|---|
| `SuracDev.Api` | ASP.NET Core 10 API. Owns the database, auth, content, PDF export, application links. |
| `SuracDev.Api.Tests` | xUnit. Domain unit tests, integration tests through the HTTP pipeline on PostgreSQL (Testcontainers). |
| `SuracDev.Web` | Angular 22, SSR, zoneless, Signals. Public site, admin area, Playwright E2E. |
| `deploy/` | Caddy site block, backup/healthwatch scripts and systemd units for the production host. |

Solution file: `SuracDev.slnx`. Containers: `surac-dev-api`, `surac-dev-web`, and in production
`surac-dev-db`. Host path `/opt/surac-dev`.

## Request flow

```
Browser ──HTTPS──▶ Caddy (host, TLS, HSTS, www→apex, security headers)
                     │
                     ▼ 127.0.0.1:<port>
                   surac-dev-web (nginx + Angular SSR node process in one container)
                     ├── /api/*  ──▶ surac-dev-api:8080
                     ├── /b/*, /, /en/*, /ueber-mich, … ──▶ SSR (node) ──▶ surac-dev-api:8080
                     └── static assets, fonts (immutable, versioned file names)
                   surac-dev-api ──▶ PostgreSQL (prod: surac-dev-db container; staging: shared dev instance)
                                 ──▶ OTLP exporter (traces/logs/metrics → Seq, optional, off when unconfigured)
```

Staging uses the same images and compose file without the `prod` profile and without Caddy.

`src/server.ts` also forwards `/api/*` to the API itself (`API_BASE_URL`), so `npm run serve:ssr`
and the Playwright suite work without nginx; in the containers nginx answers `/api/` first and the
node proxy is never reached.

## Backend layout (`SuracDev.Api`)

```
SuracDev.Api/
  Program.cs                     composition root, middleware order, startup migration + seed
  Data/
    SuracDevDbContext.cs         all EF mapping inline, snake_case tables, PascalCase columns
    SuracDevDbContextFactory.cs  design-time factory for `dotnet ef`
    Migrations/                  generated only
  Domains/
    Identity/                    AdminUser, login, password change, security stamp
    Profiles/                    the person: name, title line, intro, contact, photo, fact bar
    Career/                      Experience and Education entries
    Projects/                    Project cards and case studies
    Skills/                      SkillGroup, Skill (with "since year", never a percentage)
    Interests/                   the "Über mich" page content
    Documents/                   uploaded PDFs (CV variants are generated, not uploaded)
    Applications/                Application (mini ATS), ApplicationLink, LinkEvent, profiles
    Legal/                       Impressum and Datenschutz as Markdown blocks
    Pdf/                         QuestPDF CV documents (Public/Full × de/en)
    Health/                      /api/health
  Shared/
    Application/Exceptions/      DomainException hierarchy
    Application/Localization/    Locale value object, translation fallback helpers
    Application/Validation/      shared input rules (text limits, URLs, years, enum sentinel)
    Application/Markdown/        the restricted Markdown renderer (case studies, legal pages)
    Infrastructure/              GlobalExceptionHandler, SecurityStampValidator, Seeding, Telemetry
```

Each domain folder holds `Domain/` (entities, value objects, enums), `Application/` (use-case
services), `Contracts/` (sealed record DTOs) and `Controllers/`. Controllers are thin: bind, call one
service method, return. Services use the `DbContext` directly and throw the shared exceptions.
Domain folders are plural where the entity name would otherwise collide with the namespace
(`Profiles/` holds `Profile`).

Each content domain has a public controller (`/api/public/...`, anonymous, one locale per request
via `?locale=de|en`) and an admin controller (`/api/admin/...`, REST CRUD). Public responses carry
`fallbackFields`; admin DTOs carry both locales side by side (`de`, `en` or null) because the editor
shows them that way. Markdown is rendered to HTML on the server for public responses and stays
Markdown in admin responses.

Two OpenAPI documents: `public` (anonymous read endpoints, application-link endpoints, PDF
download) and `admin` (everything behind the cookie). The Angular app generates one client per
document.

## Frontend layout (`SuracDev.Web`)

```
SuracDev.Web/
  src/
    main.ts, main.server.ts, server.ts      bootstrap; server.ts is a plain node:http server
    app/
      app.config.ts / app.config.server.ts  providers (zoneless, hydration, router, http)
      app.routes.ts / app.routes.server.ts  routes and render modes
      core/                                 auth state, interceptors, locale, tokens, SEO helpers
      ui/                                   design system: presentational components only
      layout/                               site header, footer, admin shell
      public/                               start page, case study, über-mich, application-link view, legal
      admin/                                login, content editors, documents, application links, statistics
      api/generated/                        NSwag clients (public.client.ts, admin.client.ts), never edited
    styles/                                 _tokens.scss, _typography.scss, _layout.scss, styles.scss
    i18n/                                   UI strings de/en (content comes from the API)
  public/fonts/                             self-hosted Source Serif 4 (woff2 + OFL)
  e2e/                                      Playwright tests and run script
```

Routes: `/` and `/en/` (start page), `/projekte/<slug>` and `/en/projects/<slug>`, `/ueber-mich`
and `/en/about`, `/b/<token>` and `/en/b/<token>` (application link: the start page in the link's
section order, with its greeting and downloads), `/impressum`, `/datenschutz`, `/admin/**`
(client-rendered, no SSR, excluded from robots), `/styleguide` (development build only).

Rendering: public routes are server-rendered with hydration; the admin area is client-only. The
locale comes from the URL prefix, never from a cookie.

## Database conventions

- PostgreSQL 17. Tables `snake_case`, columns keep the PascalCase property names (quoted), as in the
  maintainer's other projects.
- Identity columns via `UseIdentityByDefaultColumn()`. Timestamps are `timestamptz`
  (`DateTimeOffset`), dates are `date` (`DateOnly`).
- Enums are stored as `int` with `HasConversion<int>()`. `0` is always `Unknown` and never written;
  shipped values are frozen.
- Translations: `<entity>_translations` with a unique index on (`<Entity>Id`, `Locale`); `Locale` is
  a two-letter code stored as `varchar(2)` with a check constraint (`de`, `en`).
- Migrations are generated with `dotnet ef`, applied with `MigrateAsync()` at startup, followed by an
  idempotent seeder that only fills empty tables.

## Cross-cutting

- **Errors**: services throw `DomainValidationException` (400), `DomainConflictException` (409),
  `DomainNotFoundException` (404); `GlobalExceptionHandler` maps them to ProblemDetails. Unique and
  foreign-key violations that slip past the services' own checks become 409 without database
  details (the constraint name goes to the log). Anything else is a 500 and a logged exception.
- **Auth**: cookie `suracdev.auth`, `FallbackPolicy = DefaultPolicy` (deny by default),
  `[AllowAnonymous]` only on deliberately public endpoints. See `docs/security-and-privacy.md`.
- **Rate limiting**: fixed-window limiter on login and on application-link resolution.
- **Telemetry**: OpenTelemetry (ASP.NET Core, HttpClient and Npgsql traces, logs, metrics; OTLP
  exporter) plus the usage events: request log middleware, page-view beacon, login lines
  (`docs/ops-and-deploy.md`, "Telemetry"). Export is disabled when no endpoint is configured, so
  local runs and tests stay silent.
- **Health**: `GET /api/health` checks database connectivity; used by the Docker healthcheck and
  uptime monitoring.
