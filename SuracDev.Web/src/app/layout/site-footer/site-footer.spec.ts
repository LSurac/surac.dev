import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { SiteFooter, SOURCE_REPOSITORY_URL } from './site-footer';

describe('app-site-footer', () => {
  it('links to the legal pages of the current locale and to the source code', async () => {
    await render(SiteFooter, {
      inputs: { locale: 'en', updatedOn: '2026-11' },
      providers: [provideRouter([])],
    });

    expect(screen.getByRole('link', { name: 'Legal notice' })).toHaveAttribute('href', '/en/impressum');
    expect(screen.getByRole('link', { name: 'Privacy' })).toHaveAttribute('href', '/en/datenschutz');
    expect(screen.getByRole('link', { name: 'Source code of this site' })).toHaveAttribute(
      'href',
      SOURCE_REPOSITORY_URL,
    );
    expect(screen.getByText('Updated: 2026-11')).toBeInTheDocument();
  });

  it('uses root paths for German', async () => {
    await render(SiteFooter, { inputs: { locale: 'de' }, providers: [provideRouter([])] });

    expect(screen.getByRole('link', { name: 'Impressum' })).toHaveAttribute('href', '/impressum');
  });
});
