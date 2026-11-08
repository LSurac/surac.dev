import { Routes } from '@angular/router';
import { adminGuard } from './core/admin-guard';
import { LoginPage } from './login/login-page';
import { AdminShell } from './shell/admin-shell';

/**
 * The admin area, loaded lazily from `app.routes.ts` and rendered in the browser only
 * (`app.routes.server.ts`). Paths are German like the rest of the site's URLs.
 */
export const adminRoutes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: AdminShell,
    canActivate: [adminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'profil' },
      {
        path: 'profil',
        loadComponent: () => import('./profile/profile-editor').then((m) => m.ProfileEditor),
      },
      {
        path: 'werdegang',
        loadComponent: () => import('./experiences/experience-editor').then((m) => m.ExperienceEditor),
      },
      {
        path: 'ausbildung',
        loadComponent: () => import('./education/education-editor').then((m) => m.EducationEditor),
      },
      {
        path: 'kenntnisse',
        loadComponent: () => import('./skills/skill-editor').then((m) => m.SkillEditor),
      },
      {
        path: 'ueber-mich',
        loadComponent: () => import('./interests/interest-editor').then((m) => m.InterestEditor),
      },
      {
        path: 'rechtstexte',
        loadComponent: () => import('./legal/legal-editor').then((m) => m.LegalEditor),
      },
      {
        path: 'projekte',
        loadComponent: () => import('./projects/project-editor').then((m) => m.ProjectEditor),
      },
      {
        path: 'dokumente',
        loadComponent: () => import('./documents/document-manager').then((m) => m.DocumentManager),
      },
      {
        path: 'bewerbungen',
        loadComponent: () => import('./applications/application-editor').then((m) => m.ApplicationEditor),
      },
      {
        path: 'statistik',
        loadComponent: () => import('./statistics/statistics-page').then((m) => m.StatisticsPage),
      },
    ],
  },
];
