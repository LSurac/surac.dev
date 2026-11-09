import { Location } from '@angular/common';
import { computed, inject, Injectable } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';
import { Locale, localeFromUrl } from './locale';

/**
 * The current URL and locale as signals, derived from the router on the server and in the browser
 * alike. `router.url` itself is not reactive, so anything computed from the URL reads it here.
 * The start value comes from `Location`: until the first navigation ends the router reports "/",
 * and an English page hydrated with German labels for a moment (its header wrapped and shifted
 * the page).
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.location.path() || this.router.url },
  );

  readonly locale = computed<Locale>(() => localeFromUrl(this.url()));
}
