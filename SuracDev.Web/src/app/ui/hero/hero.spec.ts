import { render, screen } from '@testing-library/angular';
import { UiHero } from './hero';

describe('ui-hero', () => {
  const inputs = {
    name: 'Max Mustermann',
    titleLine: 'Fullstack-Entwickler · .NET & Angular',
    headline: 'Ich baue Software, die im Betrieb bestehen muss.',
    location: 'Musterstadt',
    email: 'max@example.org',
    emailLabel: 'E-Mail schreiben',
    links: [
      { label: 'GitHub', href: 'https://github.com/example' },
      { label: 'LinkedIn', href: 'https://linkedin.com/in/example' },
    ],
  };

  it('puts name, title line, headline, location and the e-mail action above the fold', async () => {
    await render(UiHero, { inputs });

    expect(screen.getByRole('heading', { level: 1, name: 'Max Mustermann' })).toBeInTheDocument();
    expect(screen.getByText('Fullstack-Entwickler · .NET & Angular')).toBeInTheDocument();
    expect(screen.getByText('Ich baue Software, die im Betrieb bestehen muss.')).toBeInTheDocument();
    expect(screen.getByText('Musterstadt')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'E-Mail schreiben' })).toHaveAttribute(
      'href',
      'mailto:max@example.org',
    );
    expect(screen.getByRole('link', { name: 'GitHub' })).toHaveAttribute(
      'href',
      'https://github.com/example',
    );
  });

  it('offers a download as primary action and shows the photo when given', async () => {
    await render(UiHero, {
      inputs: {
        ...inputs,
        download: {
          label: 'Lebenslauf (PDF)',
          href: '/api/public/cv.pdf?locale=de',
          fileName: 'Lebenslauf.pdf',
        },
        photoUrl: '/api/public/documents/7',
        photoAlt: 'Porträt von Max Mustermann',
      },
    });

    expect(screen.getByRole('link', { name: 'Lebenslauf (PDF)' })).toHaveAttribute(
      'download',
      'Lebenslauf.pdf',
    );
    expect(screen.getByRole('img', { name: 'Porträt von Max Mustermann' })).toHaveAttribute(
      'src',
      '/api/public/documents/7',
    );
  });

  it('renders no image slot without a photo', async () => {
    await render(UiHero, { inputs });

    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});
