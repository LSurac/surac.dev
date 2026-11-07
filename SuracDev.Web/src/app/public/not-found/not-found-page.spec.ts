import { RESPONSE_INIT } from '@angular/core';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { NotFoundPage } from './not-found-page';

describe('not-found page', () => {
  it('says so, answers 404 and links back to the start page', async () => {
    const response = { status: 200 };
    await render(NotFoundPage, {
      providers: [provideRouter([]), { provide: RESPONSE_INIT, useValue: response }],
    });

    expect(screen.getByRole('heading', { level: 1, name: 'Seite nicht gefunden' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Zur Startseite' })).toHaveAttribute('href', '/');
    expect(response.status).toBe(404);
  });

  it('works without a server response in the browser', async () => {
    await render(NotFoundPage, { providers: [provideRouter([])] });

    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
  });
});
