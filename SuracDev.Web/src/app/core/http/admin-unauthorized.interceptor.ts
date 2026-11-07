import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

const ADMIN_API = '/api/admin/';

/**
 * Sends the admin back to the login when the session expired mid-edit (an admin request answered
 * 401). Registered globally because the generated clients use the root HttpClient; it deliberately
 * imports nothing from the admin area, so no admin code reaches the public bundle.
 */
export const adminUnauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && request.url.includes(ADMIN_API)) {
        void router.navigateByUrl('/admin/login');
      }
      return throwError(() => error);
    }),
  );
};
