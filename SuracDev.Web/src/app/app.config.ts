import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeDe from '@angular/common/locales/de';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { adminUnauthorizedInterceptor } from './core/http/admin-unauthorized.interceptor';

registerLocaleData(localeDe);

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'de-DE' },
    provideBrowserGlobalErrorListeners(),
    // Includes the HTTP transfer cache: what the server fetched is replayed in the browser.
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([adminUnauthorizedInterceptor])),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
  ],
};
