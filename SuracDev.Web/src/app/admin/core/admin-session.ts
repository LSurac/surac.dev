import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthClient, LoginStateResponse, SessionResponse } from '../../api/generated/admin.client';

/**
 * The admin's session as the client knows it. The cookie itself is HttpOnly; this only remembers
 * whether the API accepted it, so the guard does not ask on every navigation.
 */
@Injectable({ providedIn: 'root' })
export class AdminSession {
  private readonly auth = inject(AuthClient);
  private readonly current = signal<SessionResponse | null>(null);

  readonly session = this.current.asReadonly();

  /** True when a session exists or the cookie still holds one; never throws. */
  async restore(): Promise<boolean> {
    if (this.current()) {
      return true;
    }

    try {
      this.current.set(await firstValueFrom(this.auth.me()));
      return true;
    } catch {
      return false;
    }
  }

  /** Whether a password exists yet; the first login sets it. */
  loginState(): Promise<LoginStateResponse> {
    return firstValueFrom(this.auth.state());
  }

  /** Logs in; rejects with the client's error on a wrong password or a lockout. */
  async login(password: string): Promise<SessionResponse> {
    const session = await firstValueFrom(this.auth.login({ password }));
    this.current.set(session);
    return session;
  }

  /** Ends the session on the server and forgets it here. */
  async logout(): Promise<void> {
    await firstValueFrom(this.auth.logout());
    this.current.set(null);
  }
}
