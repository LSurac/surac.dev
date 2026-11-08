import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { adminStrings } from '../admin-strings';
import { AdminSession } from '../core/admin-session';

/** The frame of the admin area: navigation to every editor, the way back to the site, logout. */
@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShell {
  private readonly session = inject(AdminSession);
  private readonly router = inject(Router);

  protected readonly strings = adminStrings;
  protected readonly links = [
    { label: adminStrings.nav.profile, path: '/admin/profil' },
    { label: adminStrings.nav.experiences, path: '/admin/werdegang' },
    { label: adminStrings.nav.education, path: '/admin/ausbildung' },
    { label: adminStrings.nav.projects, path: '/admin/projekte' },
    { label: adminStrings.nav.skills, path: '/admin/kenntnisse' },
    { label: adminStrings.nav.legal, path: '/admin/rechtstexte' },
    { label: adminStrings.nav.documents, path: '/admin/dokumente' },
    { label: adminStrings.nav.applications, path: '/admin/bewerbungen' },
    { label: adminStrings.nav.statistics, path: '/admin/statistik' },
  ];

  protected async logout(): Promise<void> {
    await this.session.logout();
    await this.router.navigateByUrl('/admin/login');
  }
}
