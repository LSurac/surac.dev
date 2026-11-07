import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { SiteHeader } from './site-header';

describe('app-site-header', () => {
  it('shows the German navigation and links to the English version of the same page', async () => {
    await render(SiteHeader, { inputs: { locale: 'de' }, providers: [provideRouter([])] });

    expect(screen.getByRole('link', { name: 'Leonard Surac' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'Projekte' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'English' })).toHaveAttribute('href', '/en');
  });

  it('shows the English navigation and links back to German', async () => {
    await render(SiteHeader, { inputs: { locale: 'en' }, providers: [provideRouter([])] });

    expect(screen.getByRole('link', { name: 'Leonard Surac' })).toHaveAttribute('href', '/en');
    expect(screen.getByRole('link', { name: 'Projects' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Deutsch' })).toHaveAttribute('href', '/');
  });
});
