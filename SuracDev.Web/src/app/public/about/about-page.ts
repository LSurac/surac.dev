import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { Seo } from '../../core/seo/seo';
import { UiProse } from '../../ui/prose/prose';
import { UiSectionHeading } from '../../ui/section-heading/section-heading';
import { failureOf, valueOf } from '../content/load-state';
import { documentUrl, PublicContent } from '../content/public-content';
import { UnavailableNotice } from '../unavailable/unavailable-notice';

/** One interest prepared for the page. */
interface InterestView {
  readonly id: number;
  /** "01", "02", … like the numbered sections of the start page. */
  readonly number: string;
  readonly title: string;
  readonly bodyHtml: string | null;
  /** `eager` marks the first photo of the page: loaded with high priority, never lazily. */
  readonly image: { readonly url: string; readonly alt: string; readonly eager: boolean } | null;
  /** Photos alternate sides: every second section with a photo shows it before the text. */
  readonly imageFirst: boolean;
}

const PATHS = { de: '/ueber-mich', en: '/en/about' } as const;

/**
 * "Über mich" / "About me": the personality the start page leaves out on purpose (docs/decisions.md,
 * "A separate Über mich page carries personality"). One section per visible interest, in the
 * admin's order, each with an optional image.
 */
@Component({
  selector: 'app-about-page',
  imports: [UiProse, UiSectionHeading, UnavailableNotice],
  templateUrl: './about-page.html',
  styleUrl: './about-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutPage {
  private readonly content = inject(PublicContent);
  private readonly seo = inject(Seo);

  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  protected readonly strings = computed(() => uiStrings(this.locale()));

  private readonly interestsResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.interests(params),
  });

  protected readonly failure = computed(() => failureOf(this.interestsResource));
  protected readonly interests = computed<InterestView[]>(() => {
    let photos = 0;
    return (valueOf(this.interestsResource) ?? []).map((interest, index) => {
      const hasPhoto = !!interest.imageDocumentId;
      const photoIndex = hasPhoto ? photos++ : -1;
      return {
        id: interest.id ?? 0,
        number: String(index + 1).padStart(2, '0'),
        title: interest.title ?? '',
        bodyHtml: interest.bodyHtml ?? null,
        image: hasPhoto
          ? {
              url: documentUrl(interest.imageDocumentId!),
              alt: interest.imageAlt ?? interest.title ?? '',
              eager: photoIndex === 0,
            }
          : null,
        imageFirst: photoIndex % 2 === 1,
      };
    });
  });

  constructor() {
    effect(() => {
      const strings = this.strings();
      this.seo.apply({
        title: `${strings.about.title} – ${strings.siteName}`,
        description: strings.about.lede,
        locale: this.locale(),
        paths: PATHS,
      });
    });
  }
}
