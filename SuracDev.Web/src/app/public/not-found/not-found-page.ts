import { ChangeDetectionStrategy, Component, computed, inject, RESPONSE_INIT } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { uiStrings } from '../../core/i18n/ui-strings';
import { LocaleService } from '../../core/locale/locale.service';

/**
 * Shown for unknown URLs, unknown or hidden projects and missing legal pages. On the server it
 * turns the response into a real 404, so search engines drop the URL instead of indexing an empty
 * page.
 */
@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  templateUrl: './not-found-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {
  private readonly locale = inject(LocaleService).locale;

  protected readonly strings = computed(() => uiStrings(this.locale()));
  protected readonly homePath = computed(() => (this.locale() === 'de' ? '/' : '/en'));

  constructor() {
    const response = inject(RESPONSE_INIT, { optional: true });
    if (response) {
      response.status = 404;
    }
    inject(Title).setTitle(`${this.strings().notFound.title} – ${this.strings().siteName}`);
  }
}
