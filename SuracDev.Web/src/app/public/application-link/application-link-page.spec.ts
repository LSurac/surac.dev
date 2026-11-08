import { DOCUMENT, RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import {
  PublicApplicationLink,
  PublicApplicationLinksClient,
  PublicCareerClient,
  PublicProfileClient,
  PublicProjectsClient,
  PublicSkillsClient,
} from '../../api/generated/public.client';
import { ApplicationLinkPage } from './application-link-page';

const link: PublicApplicationLink = {
  greeting: 'Für das Team Backend',
  sections: ['Hero', 'Facts', 'Projects', 'WorkingMode', 'Skills', 'Career', 'Intro', 'Contact'],
  fullCv: true,
  documents: [{ id: 3, label: 'Anschreiben', fileName: 'anschreiben.pdf', kind: 'CoverLetter' }],
};

async function renderPage(get: () => unknown, locale: 'de' | 'en' = 'de') {
  const response = { status: 200 };
  const links = { get: vi.fn(get) };
  await render(ApplicationLinkPage, {
    inputs: { token: 'abc_DEF-123', locale },
    providers: [
      provideRouter([]),
      { provide: PublicApplicationLinksClient, useValue: links },
      {
        provide: PublicProfileClient,
        useValue: {
          get: () =>
            of({
              fullName: 'Max Mustermann',
              email: 'max@example.org',
              location: 'Musterstadt',
              titleLine: 'Entwickler',
              headline: 'Überschrift',
              intro: 'Kurzprofil.',
              facts: [],
              fallbackFields: [],
            }),
        },
      },
      {
        provide: PublicCareerClient,
        useValue: { listExperiences: () => of([]), listEducation: () => of([]) },
      },
      { provide: PublicProjectsClient, useValue: { list: () => of([]) } },
      { provide: PublicSkillsClient, useValue: { list: () => of([]) } },
      { provide: RESPONSE_INIT, useValue: response },
    ],
  });
  return { links, response };
}

describe('application link page', () => {
  it('shows the start page with the greeting, section order and downloads of the link', async () => {
    const { links } = await renderPage(() => of(link));

    expect(await screen.findByRole('heading', { level: 1, name: 'Max Mustermann' })).toBeInTheDocument();
    expect(links.get).toHaveBeenCalledWith('abc_DEF-123', 'de');
    expect(screen.getByText('Für das Team Backend')).toBeInTheDocument();
    expect(
      screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent?.trim()),
    ).toEqual(['01 Arbeitsweise', '02 Profil', '03 Kontakt']);
    expect(screen.getByRole('link', { name: 'Lebenslauf (PDF)' })).toHaveAttribute(
      'href',
      '/api/links/abc_DEF-123/cv.pdf?locale=de',
    );
    expect(
      within(screen.getByRole('list', { name: 'Unterlagen' })).getByRole('link', { name: 'Anschreiben' }),
    ).toHaveAttribute('href', '/api/links/abc_DEF-123/documents/3?locale=de');
  });

  it('opens the link in English with English downloads', async () => {
    const { links } = await renderPage(() => of(link), 'en');

    await screen.findByRole('heading', { level: 1 });

    expect(links.get).toHaveBeenCalledWith('abc_DEF-123', 'en');
    expect(screen.getByRole('link', { name: 'CV (PDF)' })).toHaveAttribute(
      'href',
      '/api/links/abc_DEF-123/cv.pdf?locale=en',
    );
  });

  it('keeps the link out of search engines', async () => {
    await renderPage(() => of(link));

    await screen.findByRole('heading', { level: 1 });

    expect(TestBed.inject(DOCUMENT).head.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe(
      'noindex',
    );
  });

  it('answers an unknown, expired or revoked link with the not-found page and status 404', async () => {
    const { response } = await renderPage(() => throwError(() => ({ status: 404, title: 'Not found.' })));

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Seite nicht gefunden' }),
    ).toBeInTheDocument();
    expect(screen.queryByText('Max Mustermann')).not.toBeInTheDocument();
    expect(response.status).toBe(404);
  });
});
