import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LegalPageKey } from '../../api/generated/public.client';
import { uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { Seo } from '../../core/seo/seo';
import { UiProse } from '../../ui/prose/prose';
import { PublicContent } from '../content/public-content';
import { failureOf, valueOf } from '../content/load-state';
import { NotFoundPage } from '../not-found/not-found-page';
import { UnavailableNotice } from '../unavailable/unavailable-notice';

const PATHS: Readonly<Record<string, Readonly<Record<Locale, string>>>> = {
  Imprint: { de: '/impressum', en: '/en/impressum' },
  Privacy: { de: '/datenschutz', en: '/en/datenschutz' },
};

/**
 * Impressum or privacy page. The Impressum shows the provider block (name, postal address,
 * e-mail) that the API takes from the profile, followed by the Markdown body and the date of the
 * shown locale.
 */
@Component({
  selector: 'app-legal-page',
  imports: [NotFoundPage, UiProse, UnavailableNotice],
  templateUrl: './legal-page.html',
  styleUrl: './legal-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalPage {
  private readonly content = inject(PublicContent);
  private readonly seo = inject(Seo);

  /** Bound from the route data by withComponentInputBinding. */
  readonly pageKey = input.required<LegalPageKey>();
  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  protected readonly strings = computed(() => uiStrings(this.locale()));

  private readonly pageResource = rxResource({
    params: () => ({ key: this.pageKey(), locale: this.locale() }),
    stream: ({ params }) => this.content.legalPage(params.key, params.locale),
  });

  protected readonly failure = computed(() => failureOf(this.pageResource));
  protected readonly page = computed(() => valueOf(this.pageResource));
  protected readonly contact = computed(() => this.page()?.contact ?? null);
  protected readonly updated = computed(
    () => `${this.strings().legal.updated}: ${formatLongDate(this.page()?.updatedAt, this.locale())}`,
  );

  constructor() {
    effect(() => {
      const page = this.page();
      if (page) {
        this.seo.apply({
          title: `${page.title} – ${this.strings().siteName}`,
          description: page.title ?? '',
          locale: this.locale(),
          paths: PATHS[this.pageKey()] ?? { de: '/', en: '/en' },
        });
      }
    });
  }
}

/** "7. November 2026" / "7 November 2026", in the site's time zone so server and browser agree. */
function formatLongDate(iso: string | undefined, locale: Locale): string {
  if (!iso) {
    return '';
  }

  return new Intl.DateTimeFormat(locale === 'de' ? 'de-DE' : 'en-GB', {
    dateStyle: 'long',
    timeZone: 'Europe/Berlin',
  }).format(new Date(iso));
}
