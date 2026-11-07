# Security and privacy

How the site is protected, which personal data exists where, and what this public repository must
never contain. Describe mechanisms here; never put a secret value in this file.

## Authorization model

Everything requires the logged-in admin unless a controller or action opts out with
`[AllowAnonymous]`. `Program.cs` sets `FallbackPolicy = DefaultPolicy` (authenticated user), so a
forgotten attribute fails closed. A new admin controller needs no attribute at all.

Public endpoints (`[AllowAnonymous]`, on the `public` OpenAPI document):

- `GET /api/health`
- `GET /api/public/profile`, `/experiences`, `/education`, `/projects` (`?featured=true`),
  `/projects/{slug}`, `/skills`, `/legal/{key}` — all read-only, locale via query; `/interests`
  follows in phase 3
- `GET /api/public/documents/{id}` — public documents only (profile photo, case-study images);
  link-only documents answer 404 like missing ones
- `GET /api/public/cv.pdf` (the `Public` variant)
- `GET /api/links/{token}` and its sub-resources (`/cv.pdf`, `/documents/{id}`), guarded by the
  token itself
- `GET /api/auth/state` and `POST /api/auth/login` (rate limited)

`AuthorizationSweepTests` enumerates every endpoint and asserts 401 for anonymous calls unless the
endpoint is on that list. Adding to the list is a deliberate, reviewed act.

## Admin authentication

- One `AdminUser` row, seeded without a password. The first login sets the password (minimum 12
  characters); later logins verify it with `PasswordHasher` (PBKDF2, rehash on upgrade).
- Cookie `suracdev.auth`: `HttpOnly`, `Secure` (production), `SameSite=Lax`, sliding 30 days.
  API semantics: 401/403 instead of redirects.
- Security stamp claim validated on every request; password change rotates it and logs out every
  other device.
- Login is rate limited per client address (fixed window, 10 attempts per 15 minutes). Behind
  Caddy, `ForwardedHeaders` restores the client address from `X-Forwarded-For` of the trusted proxy
  only.
- Data Protection keys persist on a named volume so cookies survive container rebuilds.
- No 2FA, no OAuth, no password reset by e-mail (there is one user who can reset via the database).

## Application links

- A link is a capability URL: `/b/<token>`, token = 128 bits from `RandomNumberGenerator`, base64url.
  The database stores only the SHA-256 of the token; the clear token is displayed once at creation.
- Required expiry (default 90 days), manual revoke. Expired or revoked links answer 404, not 410, so
  the existence of a past link is not confirmed.
- Resolution is rate limited per client address to make enumeration pointless on top of the key
  space.
- A link unlocks exactly what the admin attached: the full PDF (phone, address) and the chosen
  `LinkOnly` documents. Nothing else changes.
- Events recorded per link: kind, timestamp, locale, document id. **Never** IP address, user agent,
  referrer or geolocation. This is what keeps the statistics page out of DSGVO consent territory.

## Uploads

- Allowed: PDF, PNG, JPEG, WebP; 10 MB; content sniffed, not trusted from the extension.
- Stored outside the web root under a random file name on a named volume; served through the API
  with `Content-Disposition` and the stored content type, never by nginx directly. A path from the
  database is only resolved below the storage root.
- A rejected upload leaves no file behind. A document still referenced (profile photo, project or
  interest image, application link) cannot be deleted, and one shown on the open site cannot be
  made link-only. Certificates and cover letters can never be made public.
- Images are not processed in v1 (no resizing library); the admin uploads web-sized images.

## Transport and headers

Caddy terminates TLS (Let's Encrypt), redirects `www` to apex, sets HSTS (the `.dev` TLD is
HSTS-preloaded anyway), `X-Content-Type-Options`, `Referrer-Policy: strict-origin-when-cross-origin`,
`Permissions-Policy` and a Content-Security-Policy with `default-src 'self'`, `font-src 'self'`,
`img-src 'self' data:`, no inline scripts (Angular SSR's inline state is allowed via nonce or
hash, see `docs/gotchas.md` once hit). nginx inside the web container repeats the headers for
staging, where no Caddy sits in front.

## Personal data inventory

| Data | Where | Who sees it | Legal basis / note |
|---|---|---|---|
| Name, title, CV facts, photo | `profile`, `experiences`, `education`, documents volume | public | the maintainer's own data, published by choice |
| E-mail, location (city) | `profile` | public | contact |
| Phone, street address | `profile` | full PDF via link; Impressum (address only) | § 5 DDG requires the address in the Impressum |
| Certificates, cover letters | documents volume (`LinkOnly`) | the recipient of a link | contain employer names and signatures; never public |
| Applications (company, position, status, notes) | `applications` | admin only | the maintainer's notes about his own applications |
| Link events | `link_events` | admin only | no personal data of visitors is stored |
| Admin password hash, security stamp | `admin_users` | nobody | |
| Server logs | Caddy access log (IP, path, UA, 14 days), API logs | maintainer | Art. 6 (1) f DSGVO, named in the privacy page |
| Telemetry | Seq (traces/logs, 30 days) | maintainer | no request bodies, no cookies, no tokens in spans |

No cookies on the public site. The admin cookie is strictly necessary and set only after login.
Therefore no consent banner. Fonts are self-hosted; no third-party requests from the public site.

## What this public repository must never contain

- Secrets of any kind: connection strings with passwords, API keys, tokens, SSH keys, host keys of
  private hosts, `.env` files.
- The maintainer's real content: CV data, phone number, address, photo, certificates, cover letters.
  The seed is neutral example content.
- Infrastructure facts of the private homelab: internal hostnames, IPs, Tailscale addresses, ports
  of other services, backup paths. Workflows get hosts from repository variables.
- Personal data of third parties: names of family members, colleagues, employers' internal details.

`gitleaks` runs in CI and as a pre-commit hook; `docs/build-and-test.md` explains the setup.
Anything slipped through is treated as leaked: rotate first, then rewrite history only if the
repository is still private.

## Legal pages

Impressum (§ 5 DDG) with name, address, e-mail. Privacy page (Art. 13 DSGVO) naming hosting
provider and location, server logs and retention, telemetry, the absence of cookies and tracking,
application-link events, and the rights of the data subject. Both are Markdown content in the
database (`legal_pages`), editable in the admin, reachable from every page in two clicks.
