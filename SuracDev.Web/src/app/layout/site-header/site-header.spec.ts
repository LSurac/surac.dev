import { provideRouter, Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { SiteHeader } from './site-header';

describe('app-site-header', () => {
  it('shows the German navigation and links to the English version of the same page', async () => {
    await render(SiteHeader, { inputs: { locale: 'de' }, providers: [provideRouter([])] });

    expect(screen.getByRole('link', { name: 'Leonard Surac' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'Projekte' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Über mich' })).toHaveAttribute('href', '/ueber-mich');
    expect(screen.getByRole('link', { name: 'English' })).toHaveAttribute('href', '/en');
  });

  it('shows the English navigation and links back to German', async () => {
    await render(SiteHeader, { inputs: { locale: 'en' }, providers: [provideRouter([])] });

    expect(screen.getByRole('link', { name: 'Leonard Surac' })).toHaveAttribute('href', '/en');
    expect(screen.getByRole('link', { name: 'Projects' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'About' })).toHaveAttribute('href', '/en/about');
    expect(screen.getByRole('link', { name: 'Deutsch' })).toHaveAttribute('href', '/');
  });

  it('keeps the language link on the current page after navigating', async () => {
    const { fixture } = await render(SiteHeader, {
      inputs: { locale: 'de' },
      providers: [provideRouter([{ path: 'projekte/:slug', children: [] }])],
    });

    await fixture.debugElement.injector.get(Router).navigateByUrl('/projekte/beispiel');
    fixture.detectChanges();

    expect(await screen.findByRole('link', { name: 'English' })).toHaveAttribute(
      'href',
      '/en/projects/beispiel',
    );
  });

  it('keeps a visitor of an application link on the link page', async () => {
    const { fixture } = await render(SiteHeader, {
      inputs: { locale: 'de' },
      providers: [provideRouter([{ path: 'b/:token', children: [] }])],
    });

    await fixture.debugElement.injector.get(Router).navigateByUrl('/b/abc_DEF-123');
    fixture.detectChanges();

    expect(await screen.findByRole('link', { name: 'Projekte' })).toHaveAttribute(
      'href',
      '/b/abc_DEF-123#projekte',
    );
    expect(screen.getByRole('link', { name: 'Leonard Surac' })).toHaveAttribute('href', '/b/abc_DEF-123');
    expect(screen.getByRole('link', { name: 'English' })).toHaveAttribute('href', '/en/b/abc_DEF-123');
  });
});
