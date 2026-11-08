import { APIRequestContext, expect, test } from '@playwright/test';

/**
 * Flows 3 and 4 (docs/testing.md): an application link opens with its profile and greeting and
 * unlocks its documents; a revoked or unknown link answers 404 in both locales. The application,
 * the document and the links are created through the admin API, as the admin UI would.
 */
const password = 'e2e passphrase long enough';
const pdf = Buffer.from('%PDF-1.7\n%%EOF\n');

interface CreatedLink {
  readonly token: string;
  readonly link: { readonly id: number; readonly applicationId: number };
}

async function logIn(request: APIRequestContext): Promise<void> {
  const response = await request.post('/api/auth/login', { data: { password } });
  expect(response.ok()).toBe(true);
}

async function createApplication(request: APIRequestContext, company: string): Promise<number> {
  const response = await request.post('/api/admin/applications', {
    data: { company, position: 'Fullstack-Entwickler', status: 'Sent', appliedOn: '2026-11-08' },
  });
  expect(response.status()).toBe(201);
  return ((await response.json()) as { id: number }).id;
}

async function uploadCoverLetter(request: APIRequestContext): Promise<number> {
  const response = await request.post('/api/admin/documents', {
    multipart: {
      file: { name: 'anschreiben.pdf', mimeType: 'application/pdf', buffer: pdf },
      kind: 'CoverLetter',
      visibility: 'LinkOnly',
      labelDe: 'Anschreiben',
      labelEn: 'Cover letter',
    },
  });
  expect(response.status()).toBe(201);
  return ((await response.json()) as { id: number }).id;
}

async function createLink(
  request: APIRequestContext,
  applicationId: number,
  documentIds: number[],
): Promise<CreatedLink> {
  const response = await request.post(`/api/admin/applications/${applicationId}/links`, {
    data: {
      profile: 'TechLead',
      greetingDe: 'Für das Team Backend der Beispiel AG',
      greetingEn: 'For the backend team at Beispiel AG',
      unlockFullPdf: true,
      documentIds,
    },
  });
  expect(response.status()).toBe(201);
  return (await response.json()) as CreatedLink;
}

test.describe('application link', () => {
  test('opens with its greeting and section order and unlocks its documents', async ({ page, request }) => {
    await logIn(request);
    const applicationId = await createApplication(request, `Beispiel AG ${Date.now()}`);
    const documentId = await uploadCoverLetter(request);
    const { token } = await createLink(request, applicationId, [documentId]);

    const response = await page.goto(`/b/${token}`);

    expect(response?.status()).toBe(200);
    await expect(page.getByText('Für das Team Backend der Beispiel AG')).toBeVisible();
    await expect(page.getByRole('main').getByRole('heading', { level: 2 })).toHaveText([
      /Projekte/,
      /Arbeitsweise/,
      /Kenntnisse/,
      /Werdegang/,
      /Profil/,
      /Kontakt/,
    ]);
    await expect(page.getByRole('link', { name: 'Lebenslauf (PDF)' })).toHaveAttribute(
      'href',
      `/api/links/${token}/cv.pdf?locale=de`,
    );
    const download = page.waitForEvent('download');
    await page.getByRole('list', { name: 'Unterlagen' }).getByRole('link', { name: 'Anschreiben' }).click();
    expect((await download).suggestedFilename()).toBe('anschreiben.pdf');

    await page.getByRole('link', { name: 'English' }).click();
    await expect(page).toHaveURL(new RegExp(`/en/b/${token}$`));
    await expect(page.getByText('For the backend team at Beispiel AG')).toBeVisible();

    const statistics = await (await request.get('/api/admin/application-statistics')).json();
    const activity = statistics.applications.find(
      (entry: { applicationId: number }) => entry.applicationId === applicationId,
    );
    expect(activity).toMatchObject({ opens: 2, documentDownloads: 1 });
  });

  test('answers a revoked or unknown link with 404 in both locales', async ({ page, request }) => {
    await logIn(request);
    const applicationId = await createApplication(request, `Widerrufen GmbH ${Date.now()}`);
    const { token, link } = await createLink(request, applicationId, []);
    const revoked = await request.post(`/api/admin/applications/${applicationId}/links/${link.id}/revoke`);
    expect(revoked.status()).toBe(204);

    for (const path of [`/b/${token}`, `/b/unknown-token-0000000`]) {
      const german = await page.goto(path);
      expect(german?.status()).toBe(404);
      await expect(page.getByRole('heading', { level: 1, name: 'Seite nicht gefunden' })).toBeVisible();
      await expect(page.getByText('Max Mustermann', { exact: true })).toHaveCount(0);

      const english = await page.goto(`/en${path}`);
      expect(english?.status()).toBe(404);
      await expect(page.getByRole('heading', { level: 1, name: 'Page not found' })).toBeVisible();
    }
  });
});
