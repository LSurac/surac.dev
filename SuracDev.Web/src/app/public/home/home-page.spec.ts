import { ViewportScroller } from '@angular/common';
import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import { of, throwError } from 'rxjs';
import {
  PublicCareerClient,
  PublicProfile,
  PublicProfileClient,
  PublicProjectCard,
  PublicProjectsClient,
  PublicSkillsClient,
} from '../../api/generated/public.client';
import { HomePage } from './home-page';

const profile: PublicProfile = {
  fullName: 'Max Mustermann',
  email: 'max@example.org',
  location: 'Musterstadt',
  gitHubUrl: 'https://github.com/example',
  linkedInUrl: undefined,
  photoDocumentId: 7,
  titleLine: 'Fullstack-Entwickler · .NET & Angular',
  headline: 'Beispielüberschrift.',
  intro: 'Kurzprofil in drei Sätzen.',
  availabilityLine: undefined,
  facts: [
    { value: '5', label: 'Jahre' },
    { value: '1,8', label: 'Abschluss' },
  ],
  fallbackFields: [],
};

const project: PublicProjectCard = {
  id: 1,
  slug: 'beispielprojekt',
  status: 'InProduction',
  startedYear: 2024,
  endedYear: undefined,
  liveUrl: undefined,
  repositoryUrl: 'https://github.com/example/repo',
  stackTags: ['.NET', 'Angular'],
  isFeatured: true,
  title: 'Beispielprojekt',
  teaser: 'Ein Beispiel.',
  fallbackFields: [],
};

function providers(projects: PublicProjectCard[] = [project]) {
  const clients = {
    profile: { get: vi.fn(() => of(profile)) },
    career: {
      listExperiences: vi.fn(() =>
        of([
          {
            id: 1,
            company: 'Beispiel GmbH',
            location: 'Musterstadt',
            startDate: '2022-09-01',
            endDate: undefined,
            roleTitle: 'Softwareentwickler',
            summary: undefined,
            bullets: ['Erster Punkt'],
            stackLine: '.NET',
            fallbackFields: [],
          },
        ]),
      ),
      listEducation: vi.fn(() =>
        of([
          {
            id: 2,
            institution: 'Berufsschule',
            location: undefined,
            startDate: '2019-09-01',
            endDate: '2022-06-30',
            grade: '1,8',
            degree: 'Fachinformatiker',
            summary: undefined,
            fallbackFields: [],
          },
        ]),
      ),
    },
    projects: { list: vi.fn(() => of(projects)) },
    skills: {
      list: vi.fn(() =>
        of([{ id: 1, name: 'Backend', skills: [{ name: 'C#', sinceYear: 2019 }], fallbackFields: [] }]),
      ),
    },
  };
  return {
    clients,
    providers: [
      provideRouter([]),
      { provide: PublicProfileClient, useValue: clients.profile },
      { provide: PublicCareerClient, useValue: clients.career },
      { provide: PublicProjectsClient, useValue: clients.projects },
      { provide: PublicSkillsClient, useValue: clients.skills },
    ],
  };
}

function structuredData(): Record<string, unknown> {
  const block = TestBed.inject(DOCUMENT).head.querySelector('script[type="application/ld+json"]');
  return JSON.parse(block?.textContent ?? 'null');
}

describe('app-home-page', () => {
  it('describes the person behind the site and their profiles to search engines', async () => {
    await render(HomePage, { providers: providers().providers });
    await screen.findByRole('heading', { level: 1, name: 'Max Mustermann' });

    expect(structuredData()['@graph']).toEqual([
      expect.objectContaining({
        '@type': 'Person',
        name: 'Max Mustermann',
        jobTitle: 'Fullstack-Entwickler · .NET & Angular',
        image: 'https://surac.dev/api/public/documents/7',
        sameAs: ['https://github.com/example'],
        knowsAbout: ['C#'],
      }),
      expect.objectContaining({ '@type': 'WebSite', inLanguage: 'de' }),
    ]);
  });

  it('opens with name, title line, the fact bar and the photo', async () => {
    await render(HomePage, { providers: providers().providers });

    expect(await screen.findByRole('heading', { level: 1, name: 'Max Mustermann' })).toBeInTheDocument();
    expect(screen.getByText('Fullstack-Entwickler · .NET & Angular')).toBeInTheDocument();
    expect(screen.getAllByRole('term').map((term) => term.textContent?.trim())).toEqual([
      'Jahre',
      'Abschluss',
    ]);
    expect(screen.getByRole('img', { name: 'Porträt von Max Mustermann' })).toHaveAttribute(
      'src',
      '/api/public/documents/7',
    );
    expect(screen.getByRole('link', { name: 'E-Mail schreiben' })).toHaveAttribute(
      'href',
      'mailto:max@example.org',
    );
  });

  it('offers the CV as PDF download in the page language, named by the server', async () => {
    await render(HomePage, { providers: providers().providers, inputs: { locale: 'en' } });

    const download = await screen.findByRole('link', { name: 'CV (PDF)' });

    expect(download).toHaveAttribute('href', '/api/public/cv.pdf?locale=en');
    expect(download).toHaveAttribute('download', '');
  });

  it('shows the sections in the public order with numbered headings', async () => {
    await render(HomePage, { providers: providers().providers });
    await screen.findByRole('heading', { level: 1 });

    expect(
      screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent?.trim()),
    ).toEqual(['01 Profil', '02 Werdegang', '03 Projekte', '04 Kenntnisse', '05 Arbeitsweise', '06 Kontakt']);
  });

  it('follows the section order, greeting and downloads of an application link', async () => {
    await render(HomePage, {
      providers: providers().providers,
      inputs: {
        audience: {
          sections: ['Hero', 'Facts', 'Projects', 'WorkingMode', 'Skills', 'Career', 'Intro', 'Contact'],
          greeting: 'Für das Team Backend',
          cvHref: '/api/links/abc/cv.pdf?locale=de',
          documents: [{ label: 'Anschreiben', href: '/api/links/abc/documents/3?locale=de' }],
        },
      },
    });
    await screen.findByRole('heading', { level: 1 });

    expect(
      screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent?.trim()),
    ).toEqual(['01 Projekte', '02 Arbeitsweise', '03 Kenntnisse', '04 Werdegang', '05 Profil', '06 Kontakt']);
    expect(screen.getByText('Für das Team Backend')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Lebenslauf (PDF)' })).toHaveAttribute(
      'href',
      '/api/links/abc/cv.pdf?locale=de',
    );
    expect(
      within(screen.getByRole('list', { name: 'Unterlagen' })).getByRole('link', { name: 'Anschreiben' }),
    ).toHaveAttribute('href', '/api/links/abc/documents/3?locale=de');
  });

  it('shows no greeting and no document list on the open site', async () => {
    await render(HomePage, { providers: providers().providers });
    await screen.findByRole('heading', { level: 1 });

    expect(screen.queryByRole('list', { name: 'Unterlagen' })).not.toBeInTheDocument();
  });

  it('fills career, projects, skills and contact from the API', async () => {
    await render(HomePage, { providers: providers().providers });
    await screen.findByRole('heading', { level: 1 });

    const experience = screen.getByRole('list', { name: 'Berufserfahrung' });
    expect(within(experience).getByText('09/2022 – heute')).toBeInTheDocument();
    expect(
      within(screen.getByRole('list', { name: 'Ausbildung' })).getByText('Note 1,8'),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Fallstudie lesen' })).toHaveAttribute(
      'href',
      '/projekte/beispielprojekt',
    );
    expect(screen.getByText('Im Betrieb · seit 2024')).toBeInTheDocument();
    expect(within(screen.getByRole('list', { name: 'Backend' })).getByText('C#')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Quellcode dieser Seite auf GitHub' })).toBeInTheDocument();
  });

  it('asks the API for English content and links English case studies', async () => {
    const setup = providers();
    await render(HomePage, { providers: setup.providers, inputs: { locale: 'en' } });
    await screen.findByRole('heading', { level: 1 });

    expect(setup.clients.profile.get).toHaveBeenCalledWith('en');
    expect(setup.clients.projects.list).toHaveBeenCalledWith('en', true);
    expect(screen.getByRole('link', { name: 'Read the case study' })).toHaveAttribute(
      'href',
      '/en/projects/beispielprojekt',
    );
    expect(screen.getByRole('heading', { level: 2, name: /How I work/ })).toBeInTheDocument();
  });

  it('keeps the other sections when one part of the content fails to load', async () => {
    const setup = providers();
    setup.clients.projects.list.mockReturnValue(throwError(() => ({ status: 500 })));
    await render(HomePage, { providers: setup.providers });

    expect(await screen.findByRole('heading', { level: 1, name: 'Max Mustermann' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { level: 2, name: /Projekte/ })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: /Kenntnisse/ })).toBeInTheDocument();
  });

  it('leaves out the projects section when nothing is featured', async () => {
    await render(HomePage, { providers: providers([]).providers });
    await screen.findByRole('heading', { level: 1 });

    expect(screen.queryByRole('heading', { level: 2, name: /Projekte/ })).not.toBeInTheDocument();
  });

  it('scrolls to the section named in the address once the content has loaded', async () => {
    const scroller = { scrollToAnchor: vi.fn(), setOffset: vi.fn(), scrollToPosition: vi.fn() };
    await render(HomePage, {
      providers: [
        ...providers().providers,
        { provide: ActivatedRoute, useValue: { fragment: of('projekte') } },
        { provide: ViewportScroller, useValue: scroller },
      ],
    });

    await screen.findByRole('heading', { level: 2, name: /Projekte/ });

    await vi.waitFor(() => expect(scroller.scrollToAnchor).toHaveBeenCalledWith('projekte'));
  });

  it('tells search engines who this is, in which language', async () => {
    await render(HomePage, { providers: providers().providers, inputs: { locale: 'en' } });
    await screen.findByRole('heading', { level: 1 });
    const document = TestBed.inject(DOCUMENT);

    expect(document.title).toBe('Max Mustermann – Fullstack-Entwickler · .NET & Angular');
    expect(document.documentElement.lang).toBe('en');
    expect(document.head.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://surac.dev/en',
    );
  });
});
