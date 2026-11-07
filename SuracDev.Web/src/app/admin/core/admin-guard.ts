import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminSession } from './admin-session';

/** Lets the admin area open only with a valid session; everyone else lands on the login. */
export const adminGuard: CanActivateFn = async () => {
  const session = inject(AdminSession);
  const router = inject(Router);
  return (await session.restore()) || router.createUrlTree(['/admin/login']);
};
