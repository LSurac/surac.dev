import { provideRouter, Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of } from 'rxjs';
import { AuthClient } from '../../api/generated/admin.client';
import { AdminShell } from './admin-shell';

describe('admin shell', () => {
  async function renderShell() {
    const auth = { logout: vi.fn(() => of(undefined)), me: vi.fn(), state: vi.fn(), login: vi.fn() };
    const result = await render(AdminShell, {
      providers: [provideRouter([]), { provide: AuthClient, useValue: auth }],
    });
    const router = result.fixture.debugElement.injector.get(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    return { auth, navigate };
  }

  it('links every editor and the public site', async () => {
    await renderShell();

    const nav = screen.getByRole('navigation', { name: 'Verwaltung' });

    expect(nav).toBeInTheDocument();
    for (const [name, href] of [
      ['Profil', '/admin/profil'],
      ['Werdegang', '/admin/werdegang'],
      ['Ausbildung', '/admin/ausbildung'],
      ['Projekte', '/admin/projekte'],
      ['Kenntnisse', '/admin/kenntnisse'],
      ['Über mich', '/admin/ueber-mich'],
      ['Rechtstexte', '/admin/rechtstexte'],
      ['Dokumente', '/admin/dokumente'],
      ['Bewerbungen', '/admin/bewerbungen'],
      ['Statistik', '/admin/statistik'],
    ]) {
      expect(screen.getByRole('link', { name })).toHaveAttribute('href', href);
    }
    expect(screen.getByRole('link', { name: 'Zur Website' })).toHaveAttribute('href', '/');
  });

  it('logs out and returns to the login', async () => {
    const { auth, navigate } = await renderShell();

    await userEvent.click(screen.getByRole('button', { name: 'Abmelden' }));

    expect(auth.logout).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/admin/login');
  });
});
