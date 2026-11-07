import { expect, test } from '@playwright/test';

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

  test('the API behind the site is healthy', async ({ request }) => {
    const response = await request.get('/api/health');

    expect(response.status()).toBe(200);
    expect(await response.json()).toEqual({ status: 'ok' });
  });
});
