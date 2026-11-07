import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { UiSectionHeading } from '../../ui/section-heading/section-heading';

/**
 * The start page. In the foundation it renders the shell texts only; the hero, fact bar and the
 * content sections arrive with the Profile domain in phase 1 (docs/roadmap.md).
 */
@Component({
  selector: 'app-home-page',
  imports: [UiSectionHeading],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  protected readonly strings = computed(() => uiStrings(this.locale()));
}
