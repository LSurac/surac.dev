import { ChangeDetectionStrategy, Component, computed, inject, RESPONSE_INIT } from '@angular/core';
import { uiStrings } from '../../core/i18n/ui-strings';
import { LocaleService } from '../../core/locale/locale.service';

/**
 * Shown when the API failed while rendering a page. On the server it answers 503, so a crawler
 * retries later instead of indexing an empty page.
 */
@Component({
  selector: 'app-unavailable-notice',
  template: `
    <section class="hero">
      <h1>{{ strings().unavailable.title }}</h1>
      <p class="lede">{{ strings().unavailable.text }}</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnavailableNotice {
  private readonly locale = inject(LocaleService).locale;

  protected readonly strings = computed(() => uiStrings(this.locale()));

  constructor() {
    const response = inject(RESPONSE_INIT, { optional: true });
    if (response) {
      response.status = 503;
    }
  }
}
