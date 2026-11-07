import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { App } from './app';
import { routes } from './app.routes';

describe('App shell', () => {
  it('offers a skip link to the main content before anything else', async () => {
    await render(App, { providers: [provideRouter(routes)] });

    const skipLink = screen.getByRole('link', { name: 'Zum Inhalt springen' });

    expect(skipLink).toHaveAttribute('href', '#main');
    expect(screen.getByRole('main')).toHaveAttribute('id', 'main');
  });

  it('renders header, main and footer landmarks', async () => {
    await render(App, { providers: [provideRouter(routes)] });

    expect(screen.getByRole('banner')).toBeInTheDocument();
    expect(screen.getByRole('main')).toBeInTheDocument();
    expect(screen.getByRole('contentinfo')).toBeInTheDocument();
  });

  it('leaves the site header and footer out of the admin area', async () => {
    await render(App, {
      providers: [provideRouter([{ path: 'admin/login', children: [] }])],
      initialRoute: '/admin/login',
    });

    expect(screen.queryByRole('banner')).not.toBeInTheDocument();
    expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument();
    expect(screen.getByRole('main')).toBeInTheDocument();
  });
});
