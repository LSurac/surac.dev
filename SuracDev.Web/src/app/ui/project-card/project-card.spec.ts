import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { UiProjectCard } from './project-card';

describe('ui-project-card', () => {
  it('shows title, status, teaser, stack line and links to the case study and the code', async () => {
    await render(UiProjectCard, {
      providers: [provideRouter([])],
      inputs: {
        title: 'Vereinswebsite',
        teaser: 'Website mit Mitgliederbereich.',
        status: 'Im Betrieb',
        period: 'seit 2023',
        stackTags: ['.NET', 'Angular', 'PostgreSQL'],
        caseStudyPath: '/projekte/vereinswebsite',
        caseStudyLabel: 'Fallstudie lesen',
        links: [{ label: 'Quellcode', href: 'https://github.com/example/repo' }],
      },
    });

    expect(screen.getByRole('heading', { level: 3, name: 'Vereinswebsite' })).toBeInTheDocument();
    expect(screen.getByText('Im Betrieb · seit 2023')).toBeInTheDocument();
    expect(screen.getByText('.NET · Angular · PostgreSQL')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Fallstudie lesen' })).toHaveAttribute(
      'href',
      '/projekte/vereinswebsite',
    );
    expect(screen.getByRole('link', { name: 'Quellcode' })).toHaveAttribute(
      'href',
      'https://github.com/example/repo',
    );
  });
});
