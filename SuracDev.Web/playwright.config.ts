import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end tests of the critical flows (docs/testing.md). Start them through `npm run e2e`
 * (e2e/run-e2e.ps1): it provides a throwaway PostgreSQL in Docker and passes its connection string
 * as E2E_CONNECTION_STRING. Playwright then starts the real API and the SSR server from the
 * production build, so what is tested is what ships.
 */
const apiPort = 5189;
const webPort = 4189;
const connectionString = process.env['E2E_CONNECTION_STRING'];

if (!connectionString) {
  throw new Error('E2E_CONNECTION_STRING is not set. Start the E2E tests via e2e/run-e2e.ps1.');
}

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.e2e.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: !!process.env['CI'],
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'e2e-report' }]],
  outputDir: 'e2e-results',
  use: {
    baseURL: `http://localhost:${webPort}`,
    locale: 'de-DE',
    timezoneId: 'Europe/Berlin',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'phone', use: { ...devices['Pixel 7'] } },
  ],
  webServer: [
    {
      command: `dotnet run --project ../SuracDev.Api --no-build --urls http://localhost:${apiPort}`,
      url: `http://localhost:${apiPort}/api/health`,
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Testing',
        ConnectionStrings__Default: connectionString,
      },
    },
    {
      command: 'node dist/surac-dev/server/server.mjs',
      url: `http://localhost:${webPort}/`,
      reuseExistingServer: false,
      timeout: 60_000,
      env: {
        PORT: String(webPort),
        API_BASE_URL: `http://localhost:${apiPort}`,
      },
    },
  ],
});
