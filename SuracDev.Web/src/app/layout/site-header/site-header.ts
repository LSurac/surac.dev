import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { uiStrings } from '../../core/i18n/ui-strings';
import { Locale, switchLocaleUrl } from '../../core/locale/locale';
import { LocaleService } from '../../core/locale/locale.service';

/** `/b/<token>` and `/en/b/<token>`: an application link, the start page as its recipient sees it. */
const APPLICATION_LINK_PATH = /^(\/en)?\/b\/[^/?#]+/;

/**
 * Name, section navigation and the locale switch. The switch keeps the visitor on the same page.
 * On an application link the name and the section anchors stay on the link page, so its section
 * order, greeting and downloads are not lost by a click in the header.
 */
@Component({
  selector: 'app-site-header',
  imports: [RouterLink],
  templateUrl: './site-header.html',
  styleUrl: './site-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteHeader {
  private readonly currentUrl = inject(LocaleService).url;

  readonly locale = input.required<Locale>();

  protected readonly strings = computed(() => uiStrings(this.locale()));
  protected readonly homeUrl = computed(
    () => APPLICATION_LINK_PATH.exec(this.currentUrl())?.[0] ?? (this.locale() === 'de' ? '/' : '/en'),
  );
  protected readonly aboutUrl = computed(() => (this.locale() === 'de' ? '/ueber-mich' : '/en/about'));
  protected readonly otherLocale = computed<Locale>(() => (this.locale() === 'de' ? 'en' : 'de'));
  protected readonly switchUrl = computed(() => switchLocaleUrl(this.currentUrl(), this.otherLocale()));
  protected readonly sections = computed(() => {
    const nav = this.strings().nav;
    return [
      { label: nav.projects, fragment: 'projekte' },
      { label: nav.career, fragment: 'werdegang' },
      { label: nav.workingMode, fragment: 'arbeitsweise' },
    ];
  });
}
