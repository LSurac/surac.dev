import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot, UrlTree } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiException, AuthClient } from '../../api/generated/admin.client';
import { adminGuard } from './admin-guard';

describe('adminGuard', () => {
  function runGuard(me: () => unknown) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthClient, useValue: { me: vi.fn(me) } }],
    });
    return TestBed.runInInjectionContext(() =>
      adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    ) as Promise<boolean | UrlTree>;
  }

  it('lets a logged-in admin through', async () => {
    const result = await runGuard(() => of({ id: 1, displayName: 'Admin', isInitialLogin: false }));

    expect(result).toBe(true);
  });

  it('sends everyone else to the login', async () => {
    const result = await runGuard(() =>
      throwError(() => new ApiException('Unauthorized', 401, '', {}, null)),
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect(String(result)).toBe('/admin/login');
  });
});
