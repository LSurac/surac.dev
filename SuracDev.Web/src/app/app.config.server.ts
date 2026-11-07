import { HttpBackend } from '@angular/common/http';
import { ApplicationConfig, inject, mergeApplicationConfig, REQUEST_CONTEXT } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { ApiBaseUrlBackend } from './core/http/api-base-url.backend';
import { API_BASE_URL } from './core/http/api-base-url.token';
import type { ServerRequestContext } from './core/http/server-request-context';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    { provide: HttpBackend, useClass: ApiBaseUrlBackend },
    {
      provide: API_BASE_URL,
      useFactory: () => (inject(REQUEST_CONTEXT) as ServerRequestContext | null)?.apiBaseUrl ?? '',
    },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
