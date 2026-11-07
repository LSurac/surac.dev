import { inject, Injectable } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';
import { Locale, localeFromUrl } from './locale';

/** The current locale, derived from the router URL on the server and in the browser alike. */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly router = inject(Router);

  readonly locale = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => localeFromUrl(event.urlAfterRedirects)),
    ),
    { initialValue: localeFromUrl(this.router.url) as Locale },
  );
}
