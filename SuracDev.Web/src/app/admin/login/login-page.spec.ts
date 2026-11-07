import { provideRouter, Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { ApiException, AuthClient } from '../../api/generated/admin.client';
import { LoginPage } from './login-page';

function authClient(
  hasPassword: boolean,
  login = vi.fn(() => of({ id: 1, displayName: 'Admin', isInitialLogin: false })),
) {
  return { state: vi.fn(() => of({ hasPassword })), login, me: vi.fn(), logout: vi.fn() };
}

async function renderLogin(client: ReturnType<typeof authClient>) {
  const result = await render(LoginPage, {
    providers: [provideRouter([]), { provide: AuthClient, useValue: client }],
  });
  const router = result.fixture.debugElement.injector.get(Router);
  const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  return { navigate };
}

describe('admin login page', () => {
  it('asks to set the password on the very first login', async () => {
    await renderLogin(authClient(false));

    expect(await screen.findByRole('heading', { name: 'Passwort festlegen' })).toBeInTheDocument();
    expect(screen.getByText(/mindestens 12 Zeichen/)).toBeInTheDocument();
    expect(screen.getByLabelText('Passwort')).toHaveAttribute('autocomplete', 'new-password');
  });

  it('logs in and opens the admin area', async () => {
    const client = authClient(true);
    const { navigate } = await renderLogin(client);

    await userEvent.type(await screen.findByLabelText('Passwort'), 'correct horse battery');
    await userEvent.click(screen.getByRole('button', { name: 'Anmelden' }));

    expect(client.login).toHaveBeenCalledWith({ password: 'correct horse battery' });
    expect(navigate).toHaveBeenCalledWith('/admin');
  });

  it('shows the error of a wrong password and stays on the page', async () => {
    const body = JSON.stringify({ status: 400, errors: { password: ['Wrong password.'] } });
    const client = authClient(
      true,
      vi.fn(() => throwError(() => new ApiException('Bad', 400, body, {}, null))),
    );
    const { navigate } = await renderLogin(client);

    await userEvent.type(await screen.findByLabelText('Passwort'), 'not the password');
    await userEvent.click(screen.getByRole('button', { name: 'Anmelden' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Wrong password.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('explains a lockout after too many attempts', async () => {
    const client = authClient(
      true,
      vi.fn(() => throwError(() => new ApiException('Too many', 429, '', {}, null))),
    );
    await renderLogin(client);

    await userEvent.type(await screen.findByLabelText('Passwort'), 'whatever password');
    await userEvent.click(screen.getByRole('button', { name: 'Anmelden' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Zu viele Versuche');
  });
});
