import { expect, test } from '@playwright/test';

/**
 * Flow 2 (docs/testing.md): the admin logs in, edits a text, and the public page shows it. The
 * throwaway database starts without a password, so the first run sets it and later runs log in.
 */
const password = 'e2e passphrase long enough';

test.describe('admin', () => {
  test('logs in, edits the headline and the start page shows it', async ({ page }) => {
    const headline = `Neue Überschrift ${Date.now()}`;

    await page.goto('/admin/login');
    await page.getByLabel('Passwort').fill(password);
    await page.getByRole('button', { name: /Passwort festlegen|Anmelden/ }).click();

    await expect(page).toHaveURL(/\/admin\/profil$/);
    const german = page.getByRole('group', { name: 'Deutsch' });
    await german.getByLabel('Überschrift').fill(headline);
    await page.getByRole('button', { name: 'Speichern' }).click();
    await expect(page.getByRole('status')).toHaveText('Gespeichert.');

    await page.goto('/');
    await expect(page.getByText(headline)).toBeVisible();
  });

  test('keeps the editors closed without a session', async ({ page }) => {
    await page.goto('/admin/projekte');

    await expect(page).toHaveURL(/\/admin\/login$/);
  });
});
