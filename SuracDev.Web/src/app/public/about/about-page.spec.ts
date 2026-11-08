import { DOCUMENT, RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import { PublicInterest, PublicInterestsClient } from '../../api/generated/public.client';
import { AboutPage } from './about-page';

const chess: PublicInterest = {
  id: 1,
  kind: 'Chess',
  title: 'Schach',
  bodyHtml: '<p>Spiele im <strong>Verein</strong>.</p>',
  imageDocumentId: 7,
  imageAlt: 'Schachbrett im Vereinsheim',
  fallbackFields: [],
};

const music: PublicInterest = {
  id: 2,
  kind: 'Music',
  title: 'Musik',
  bodyHtml: undefined,
  imageDocumentId: undefined,
  imageAlt: undefined,
  fallbackFields: [],
};

async function renderPage(list: () => unknown, locale: 'de' | 'en' = 'de') {
  const response = { status: 200 };
  const interests = { list: vi.fn(list) };
  await render(AboutPage, {
    inputs: { locale },
    providers: [
      provideRouter([]),
      { provide: PublicInterestsClient, useValue: interests },
      { provide: RESPONSE_INIT, useValue: response },
    ],
  });
  return { interests, response };
}

describe('about page', () => {
  it('shows every interest with its heading, image and text', async () => {
    await renderPage(() => of([chess, music]));

    expect(await screen.findByRole('heading', { level: 1, name: 'Über mich' })).toBeInTheDocument();
    expect(
      screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent?.trim()),
    ).toEqual(['Schach', 'Musik']);
    expect(screen.getByRole('img', { name: 'Schachbrett im Vereinsheim' })).toHaveAttribute(
      'src',
      '/api/public/documents/7',
    );
    expect(screen.getByText('Verein')).toBeInTheDocument();
    expect(screen.getAllByRole('img')).toHaveLength(1);
  });

  it('loads the first image first and the others lazily', async () => {
    await renderPage(() => of([chess, { ...music, imageDocumentId: 8, imageAlt: 'Am DJ-Pult' }]));

    expect(await screen.findByRole('img', { name: 'Schachbrett im Vereinsheim' })).toHaveAttribute(
      'fetchpriority',
      'high',
    );
    expect(screen.getByRole('img', { name: 'Am DJ-Pult' })).toHaveAttribute('loading', 'lazy');
  });

  it('loads the English page and tells search engines both addresses', async () => {
    const { interests } = await renderPage(() => of([chess]), 'en');

    expect(await screen.findByRole('heading', { level: 1, name: 'About me' })).toBeInTheDocument();
    expect(interests.list).toHaveBeenCalledWith('en');
    const head = TestBed.inject(DOCUMENT).head;
    expect(head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://surac.dev/en/about',
    );
    expect(head.querySelector('link[hreflang="de"]')?.getAttribute('href')).toBe(
      'https://surac.dev/ueber-mich',
    );
  });

  it('answers a failing API with a notice and status 503', async () => {
    const { response } = await renderPage(() => throwError(() => ({ status: 500 })));

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Gerade nicht erreichbar' }),
    ).toBeInTheDocument();
    expect(response.status).toBe(503);
  });
});
