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
    ],
  },
];
