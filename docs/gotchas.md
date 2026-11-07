# Gotchas

Non-obvious traps already hit once. Add the date and the symptom; keep entries short.

- **PowerShell `Select-Object -First N` on build output kills the build.** The pipeline closes
  early, the process gets a broken pipe and the step reports success. Use `Select-String`, `grep`
  or write to a file.
- **`.dev` is HSTS-preloaded.** There is no plain-HTTP `surac.dev`; a Caddy misconfiguration shows
  as a browser refusal, not as a redirect. Test TLS first, everything else second.
- **Angular 22 SSR answers 400 "Bad Request" for every host it does not know** (2026-11-07, found
  by the first E2E run: `Header "host" with value "localhost:4189" is not allowed`). The engine
  validates `Host`/`X-Forwarded-Host` against `security.allowedHosts` from angular.json plus the
  `allowedHosts` option. `src/server.ts` adds `localhost`/`127.0.0.1` by default and reads
  `NG_ALLOWED_HOSTS` for a deployment's own names; compose sets it from `SURACDEV_ALLOWED_HOSTS`.
  A new staging or preview host name has to be added there, not in the repository.
- **NSwag's Angular template needs `FileReader` on the server.** The generated client reads
  responses as blobs; Node has no `FileReader`, so SSR calls fail silently and the page renders
  empty. `server.ts` installs a minimal shim before anything else (carried over from the
  maintainer's club website, where it was found the hard way).
- **The hydration transfer cache loses blob responses** (2026-11-07, found by the first E2E run of
  the start page: the server render showed the profile, the hydrated page lost it, without a single
  request or console error). Angular serialises cached `blob` bodies with `new Uint8Array(body)`,
  which is empty for a Blob, and every NSwag call asks for a blob. `ApiBaseUrlBackend` therefore
  fetches blob responses as ArrayBuffer on the server; the cache restores them as Blob in the
  browser, and the FileReader shim in `server.ts` reads both.
- **Testcontainers needs Docker running on the workstation.** `[PostgresFact]` skips integration
  tests locally when Docker is down and never in CI. A green local run with Docker stopped is not a
  green gate.
- **The OpenAPI generation runs the API in the `OpenApi` environment** without a database. Anything
  that touches the database at startup must be guarded by `isOpenApiGeneration`, or
  `npm run api:generate` hangs on a connection attempt.
- **Compose env files take the last duplicate key.** A commented block at the end of `.env` that
  redefines a key wins. Keep `.env` free of duplicates.
- **A PowerShell-generated SSH key with `-N '""'` has a non-empty passphrase.** Generate deploy
  keys from Git Bash or WSL.
