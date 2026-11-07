import { DOCUMENT, RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import { PublicLegalClient, PublicLegalPage } from '../../api/generated/public.client';
import { LegalPage } from './legal-page';

const imprint: PublicLegalPage = {
  key: 'Imprint',
  title: 'Impressum',
  bodyHtml: '<p>Verantwortlich für den Inhalt.</p>',
  updatedAt: '2026-11-07T12:00:00Z',
  contact: {
    fullName: 'Max Mustermann',
    addressLines: ['Musterstraße 1', '00000 Musterstadt'],
    email: 'max@example.org',
  },
  fallbackFields: [],
};

async function renderPage(
  page: () => unknown,
  key: 'Imprint' | 'Privacy' = 'Imprint',
  locale: 'de' | 'en' = 'de',
) {
  const response = { status: 200 };
  const legal = { get: vi.fn(page) };
  await render(LegalPage, {
    inputs: { pageKey: key, locale },
    providers: [
      provideRouter([]),
      { provide: PublicLegalClient, useValue: legal },
      { provide: RESPONSE_INIT, useValue: response },
    ],
  });
  return { legal, response };
}

describe('legal page', () => {
  it('shows the Impressum with the provider block from the profile and the date', async () => {
    await renderPage(() => of(imprint));

    expect(await screen.findByRole('heading', { level: 1, name: 'Impressum' })).toBeInTheDocument();
    expect(screen.getByText('Max Mustermann')).toBeInTheDocument();
    expect(screen.getByText('Musterstraße 1')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'max@example.org' })).toHaveAttribute(
      'href',
      'mailto:max@example.org',
    );
    expect(screen.getByText('Verantwortlich für den Inhalt.')).toBeInTheDocument();
    expect(screen.getByText('Stand: 7. November 2026')).toBeInTheDocument();
  });

  it('shows the privacy page in English without a provider block', async () => {
    const privacy = { ...imprint, key: 'Privacy' as const, title: 'Privacy policy', contact: undefined };
    const { legal } = await renderPage(() => of(privacy), 'Privacy', 'en');

    expect(await screen.findByRole('heading', { level: 1, name: 'Privacy policy' })).toBeInTheDocument();
    expect(legal.get).toHaveBeenCalledWith('Privacy', 'en');
    expect(screen.queryByText('Max Mustermann')).not.toBeInTheDocument();
    expect(screen.getByText('Updated: 7 November 2026')).toBeInTheDocument();
    expect(TestBed.inject(DOCUMENT).head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://surac.dev/en/datenschutz',
    );
  });

  it('answers a missing page with status 404', async () => {
    const { response } = await renderPage(() => throwError(() => ({ status: 404 })));

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Seite nicht gefunden' }),
    ).toBeInTheDocument();
    expect(response.status).toBe(404);
  });
});
