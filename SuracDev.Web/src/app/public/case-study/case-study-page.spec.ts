import { DOCUMENT, RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import { PublicCaseStudy, PublicProjectsClient } from '../../api/generated/public.client';
import { CaseStudyPage } from './case-study-page';

const study: PublicCaseStudy = {
  slug: 'beispielprojekt',
  status: 'Live',
  startedYear: 2024,
  endedYear: undefined,
  liveUrl: 'https://example.org',
  repositoryUrl: undefined,
  stackTags: ['.NET', 'Angular'],
  title: 'Beispielprojekt',
  teaser: 'Ein Beispiel.',
  problem: 'Das Problem.',
  myRole: 'Meine Rolle.',
  outcome: 'Das Ergebnis.',
  caseStudyHtml: '<h2>Abschnitt</h2>\n<p>Text</p>',
  decisions: [{ title: 'Entscheidung', tradeOff: 'Abwägung' }],
  images: [{ documentId: 21, caption: 'Startseite' }],
  fallbackFields: [],
};

async function renderPage(get: () => unknown, locale: 'de' | 'en' = 'de') {
  const response = { status: 200 };
  const projects = { get: vi.fn(get) };
  await render(CaseStudyPage, {
    inputs: { slug: 'beispielprojekt', locale },
    providers: [
      provideRouter([]),
      { provide: PublicProjectsClient, useValue: projects },
      { provide: RESPONSE_INIT, useValue: response },
    ],
  });
  return { projects, response };
}

describe('case study page', () => {
  it('tells the story of the project: problem, role, decisions, case study, images and outcome', async () => {
    await renderPage(() => of(study));

    expect(await screen.findByRole('heading', { level: 1, name: 'Beispielprojekt' })).toBeInTheDocument();
    expect(screen.getByText('Live · seit 2024')).toBeInTheDocument();
    expect(screen.getByText('.NET · Angular')).toBeInTheDocument();
    expect(screen.getByText('Das Problem.')).toBeInTheDocument();
    expect(screen.getByText('Entscheidung')).toBeInTheDocument();
    expect(screen.getByText('Abwägung')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Abschnitt' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Startseite' })).toHaveAttribute(
      'src',
      '/api/public/documents/21',
    );
    expect(screen.getByText('Das Ergebnis.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Live ansehen' })).toHaveAttribute('href', 'https://example.org');
    expect(screen.getByRole('link', { name: 'Zur Startseite' })).toHaveAttribute('href', '/');
  });

  it('tells the story in order: problem, role, solution, decisions, outcome', async () => {
    await renderPage(() => of(study));

    await screen.findByRole('heading', { level: 1 });

    expect(
      screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent?.trim()),
    ).toEqual(['Problem', 'Meine Rolle', 'Fallstudie', 'Abschnitt', 'Entscheidungen', 'Ergebnis']);
  });

  it('opens the live product in a new tab', async () => {
    await renderPage(() => of(study));

    const live = await screen.findByRole('link', { name: 'Live ansehen' });

    expect(live).toHaveAttribute('target', '_blank');
    expect(live).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('loads the English version and links back to the English start page', async () => {
    const { projects } = await renderPage(() => of(study), 'en');

    await screen.findByRole('heading', { level: 1 });

    expect(projects.get).toHaveBeenCalledWith('beispielprojekt', 'en');
    expect(screen.getByRole('link', { name: 'Back to the start page' })).toHaveAttribute('href', '/en');
    expect(TestBed.inject(DOCUMENT).head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://surac.dev/en/projects/beispielprojekt',
    );
  });

  it('answers an unknown project with a not-found page and status 404', async () => {
    const { response } = await renderPage(() => throwError(() => ({ status: 404, title: 'Not found.' })));

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Seite nicht gefunden' }),
    ).toBeInTheDocument();
    expect(response.status).toBe(404);
  });

  it('answers a failing API with a notice and status 503, never with an empty 200', async () => {
    const { response } = await renderPage(() => throwError(() => ({ status: 500, title: 'Server error.' })));

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Gerade nicht erreichbar' }),
    ).toBeInTheDocument();
    expect(response.status).toBe(503);
  });
});
