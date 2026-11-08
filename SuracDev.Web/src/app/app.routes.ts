import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';
import { HomePage } from './public/home/home-page';

/**
 * Locale comes from the URL prefix, never from a cookie: `/` is German, `/en` is English
 * (docs/architecture.md). Route data is bound to component inputs (withComponentInputBinding).
 */
const caseStudy = () => import('./public/case-study/case-study-page').then((m) => m.CaseStudyPage);
const legal = () => import('./public/legal/legal-page').then((m) => m.LegalPage);
const about = () => import('./public/about/about-page').then((m) => m.AboutPage);
const applicationLink = () =>
  import('./public/application-link/application-link-page').then((m) => m.ApplicationLinkPage);

export const routes: Routes = [
  { path: '', component: HomePage, data: { locale: 'de' } },
  { path: 'en', component: HomePage, data: { locale: 'en' } },
  { path: 'projekte/:slug', loadComponent: caseStudy, data: { locale: 'de' } },
  { path: 'en/projects/:slug', loadComponent: caseStudy, data: { locale: 'en' } },
  { path: 'ueber-mich', loadComponent: about, data: { locale: 'de' } },
  { path: 'en/about', loadComponent: about, data: { locale: 'en' } },
  { path: 'impressum', loadComponent: legal, data: { locale: 'de', pageKey: 'Imprint' } },
  { path: 'datenschutz', loadComponent: legal, data: { locale: 'de', pageKey: 'Privacy' } },
  { path: 'en/impressum', loadComponent: legal, data: { locale: 'en', pageKey: 'Imprint' } },
  { path: 'en/datenschutz', loadComponent: legal, data: { locale: 'en', pageKey: 'Privacy' } },
  { path: 'b/:token', loadComponent: applicationLink, data: { locale: 'de' } },
  { path: 'en/b/:token', loadComponent: applicationLink, data: { locale: 'en' } },
  { path: 'admin', loadChildren: () => import('./admin/admin.routes').then((m) => m.adminRoutes) },
  ...(isDevMode()
    ? [
        {
          path: 'styleguide',
          loadComponent: () => import('./public/styleguide/styleguide-page').then((m) => m.StyleguidePage),
        },
      ]
    : []),
  {
    path: '**',
    loadComponent: () => import('./public/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
