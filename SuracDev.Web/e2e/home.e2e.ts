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

  test('the API behind the site is healthy', async ({ request }) => {
    const response = await request.get('/api/health');

    expect(response.status()).toBe(200);
    expect(await response.json()).toEqual({ status: 'ok' });
  });
});
