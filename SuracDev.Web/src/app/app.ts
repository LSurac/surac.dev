import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, PLATFORM_ID } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { uiStrings } from './core/i18n/ui-strings';
import { LocaleService } from './core/locale/locale.service';
import { PageViewTracker, pageViewPath } from './core/telemetry/page-view-tracker';
import { SiteFooter } from './layout/site-footer/site-footer';
import { SiteHeader } from './layout/site-header/site-header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeader, SiteFooter],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly localeService = inject(LocaleService);
  private readonly router = inject(Router);

  protected readonly locale = this.localeService.locale;
  protected readonly strings = computed(() => uiStrings(this.locale()));
  /** The admin area has its own shell; the site's header and footer stay out of it. */
  protected readonly isAdmin = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => isAdminUrl(event.urlAfterRedirects)),
    ),
    { initialValue: isAdminUrl(this.router.url) },
  );

  constructor() {
    if (!isPlatformBrowser(inject(PLATFORM_ID))) {
      return;
    }
    // Every page the browser shows, the first one included (its navigation ends after hydration).
    const pageViews = inject(PageViewTracker);
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((event) => pageViews.track(pageViewPath(event.urlAfterRedirects)));
  }
}

function isAdminUrl(url: string): boolean {
  return url === '/admin' || url.startsWith('/admin/');
}
