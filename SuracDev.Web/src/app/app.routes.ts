import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';
import { HomePage } from './public/home/home-page';

/**
 * Locale comes from the URL prefix, never from a cookie: `/` is German, `/en` is English
 * (docs/architecture.md). Route data is bound to component inputs (withComponentInputBinding).
 */
export const routes: Routes = [
  { path: '', component: HomePage, data: { locale: 'de' } },
  { path: 'en', component: HomePage, data: { locale: 'en' } },
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then((m) => m.adminRoutes) },
  ...(isDevMode()
    ? [
        {
          path: 'styleguide',
          loadComponent: () => import('./public/styleguide/styleguide-page').then((m) => m.StyleguidePage),
        },
      ]
    : []),
  { path: '**', redirectTo: '' },
];
