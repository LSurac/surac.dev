import { expect, test } from '@playwright/test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** The script-src of the CSP nginx sends (staging and production); the SSR server itself sends none. */
function cspScriptSources(): string {
  const headers = readFileSync(join(__dirname, '..', 'security-headers.conf'), 'utf8');
  return /script-src ([^;]+);/.exec(headers)?.[1] ?? '';
}

/** The sha256 sources of every inline script with code in a server-rendered page; an empty one runs nothing. */
function inlineScriptHashes(html: string): string[] {
  return [...html.matchAll(/<script(\s[^>]*)?>([\s\S]*?)<\/script>/g)]
    .filter(
      ([, attributes = '', body = '']) => body.trim() !== '' && !/\bsrc=|application\/json/.test(attributes),
    )
    .map(([, , body = '']) => `'sha256-${createHash('sha256').update(body, 'utf8').digest('base64')}'`);
}

/** Flow 1 (docs/testing.md): the start page is rendered on the server, in both locales. */
test.describe('start page', () => {
  test('renders server-side with one h1 and the landmarks', async ({ request }) => {
    const response = await request.get('/');
    const html = await response.text();

    expect(response.status()).toBe(200);
    expect(html).toContain('<h1');
    expect(html).toContain('<main');
    expect(html).toContain('lang="de"');
  });

  test('is usable in the browser and switches to English on the same page', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await page.getByRole('link', { name: 'English' }).click();

    await expect(page).toHaveURL(/\/en$/);
    await expect(page.getByRole('link', { name: 'Deutsch' })).toBeVisible();
  });

  test('renders the profile, the sections and the SEO links on the server', async ({ request }) => {
    const html = await (await request.get('/')).text();

    expect(html).toContain('Max Mustermann');
    expect(html).toContain('Fullstack-Entwickler · .NET &amp; Angular');
    expect(html).toContain('Kenntnisse');
    expect(html).toContain('hreflang="en"');
    expect(html).toContain('<title>Max Mustermann – Fullstack-Entwickler');
  });

  test('renders the content behind a proxy that sends an empty forwarded protocol', async ({ request }) => {
    // nginx forwards $http_x_forwarded_proto, which is empty when nothing sits in front of it (staging).
    const html = await (await request.get('/', { headers: { 'x-forwarded-proto': '' } })).text();

    expect(html).toContain('Max Mustermann');
  });

  test('renders the content when the proxy passes the host without its port', async ({ request }) => {
    // nginx sends `Host: $host`, which drops the port; the page origin then points at nothing.
    const html = await (await request.get('/', { headers: { host: 'localhost' } })).text();

    expect(html).toContain('Max Mustermann');
  });

  test('renders the English start page with English content and language', async ({ request }) => {
    const html = await (await request.get('/en')).text();

    expect(html).toContain('lang="en"');
    expect(html).toContain('Full-stack developer · .NET &amp; Angular');
  });

  test('opens a case study from the start page and switches it to English', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: 'Fallstudie lesen' }).first().click();

    await expect(page).toHaveURL(/\/projekte\/beispielprojekt$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Beispielprojekt' })).toBeVisible();
    await page.getByRole('link', { name: 'English' }).click();
    await expect(page).toHaveURL(/\/en\/projects\/beispielprojekt$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Example project' })).toBeVisible();
  });

  test('opens the about page from the header, rendered on the server, in both languages', async ({
    page,
    request,
  }) => {
    const html = await (await request.get('/ueber-mich')).text();
    expect(html).toContain('Beispiel-Interesse');

    await page.goto('/');
    await page.getByRole('link', { name: 'Über mich' }).click();
    await expect(page).toHaveURL(/\/ueber-mich$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Über mich' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 2, name: 'Beispiel-Interesse' })).toBeVisible();
    await page.getByRole('link', { name: 'English' }).click();
    await expect(page).toHaveURL(/\/en\/about$/);
    await expect(page.getByRole('heading', { level: 2, name: 'Example interest' })).toBeVisible();
  });

  test('scrolls to a section of the start page when its header link is used on another page', async ({
    page,
  }) => {
    await page.goto('/ueber-mich');
    await page.getByRole('navigation').getByRole('link', { name: 'Projekte' }).click();

    await expect(page).toHaveURL(/\/#projekte$/);
    await expect(page.locator('#projekte')).toBeInViewport();
  });

  test('downloads the CV as PDF under the name the server gives it', async ({ page }) => {
    await page.goto('/');
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('link', { name: 'Lebenslauf (PDF)' }).click(),
    ]);

    expect(download.suggestedFilename()).toBe('Lebenslauf_Max_Mustermann.pdf');
  });

  test('answers unknown pages and unknown projects with 404', async ({ request }) => {
    expect((await request.get('/gibt-es-nicht')).status()).toBe(404);
    expect((await request.get('/projekte/gibt-es-nicht')).status()).toBe(404);
  });

  test('reaches the Impressum with the provider block from the footer', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: 'Impressum' }).click();

    await expect(page.getByRole('heading', { level: 1, name: 'Impressum' })).toBeVisible();
    await expect(page.getByText('Musterstraße 1')).toBeVisible();
  });

  test('publishes robots.txt and a sitemap with the case studies', async ({ request }) => {
    const robots = await request.get('/robots.txt');
    const sitemap = await request.get('/sitemap.xml');
    const xml = await sitemap.text();

    expect(await robots.text()).toContain('Sitemap: https://surac.dev/sitemap.xml');
    expect(sitemap.headers()['content-type']).toContain('application/xml');
    expect(xml).toContain('<loc>https://surac.dev/projekte/beispielprojekt</loc>');
    expect(xml).toContain('hreflang="en" href="https://surac.dev/en/projects/beispielprojekt"');
  });

  test('opens every link to another site in a new tab', async ({ page }) => {
    for (const path of ['/', '/en', '/ueber-mich', '/projekte/beispielprojekt', '/impressum']) {
      await page.goto(path);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      const external = await page.locator('a[href^="http"]').evaluateAll((links) =>
        links
          .filter((link) => new URL((link as HTMLAnchorElement).href).origin !== location.origin)
          .map((link) => ({
            href: (link as HTMLAnchorElement).href,
            target: link.getAttribute('target'),
            rel: link.getAttribute('rel') ?? '',
          })),
      );
      expect(external.length, path).toBeGreaterThan(0);
      for (const link of external) {
        expect(link.target, `${path} ${link.href}`).toBe('_blank');
        expect(link.rel, `${path} ${link.href}`).toContain('noopener');
      }
    }
  });

  test('allows every inline script of the server render in the CSP', async ({ request }) => {
    const allowed = cspScriptSources();

    for (const path of ['/', '/en', '/ueber-mich', '/impressum', '/projekte/beispielprojekt', '/b/unknown']) {
      const hashes = inlineScriptHashes(await (await request.get(path)).text());
      expect(hashes.length, path).toBeGreaterThan(0);
      for (const hash of hashes) {
        expect(allowed, `${path} needs ${hash} in security-headers.conf`).toContain(hash);
      }
    }
  });

  test('the API behind the site is healthy', async ({ request }) => {
    const response = await request.get('/api/health');

    expect(response.status()).toBe(200);
    expect(await response.json()).toEqual({ status: 'ok' });
  });
});
