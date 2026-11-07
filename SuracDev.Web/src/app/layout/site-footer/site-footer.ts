import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';

export const SOURCE_REPOSITORY_URL = 'https://github.com/LSurac/surac.dev';

/** Legal links, the link to this repository and the last-updated date. */
@Component({
  selector: 'app-site-footer',
  imports: [RouterLink],
  templateUrl: './site-footer.html',
  styleUrl: './site-footer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteFooter {
  readonly locale = input.required<Locale>();
  readonly updatedOn = input<string>('2026-11');

  protected readonly sourceUrl = SOURCE_REPOSITORY_URL;
  protected readonly strings = computed(() => uiStrings(this.locale()));
  protected readonly prefix = computed(() => (this.locale() === 'de' ? '' : '/en'));
}
