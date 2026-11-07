import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PublicCaseStudy } from '../../api/generated/public.client';
import { UiStrings, uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { Seo } from '../../core/seo/seo';
import { UiProse } from '../../ui/prose/prose';
import { documentUrl, PublicContent } from '../content/public-content';
import { failureOf, valueOf } from '../content/load-state';
import { NotFoundPage } from '../not-found/not-found-page';
import { UnavailableNotice } from '../unavailable/unavailable-notice';

/** A link below the case study title (live product, source code). */
interface StudyLink {
  readonly label: string;
  readonly href: string;
}

/**
 * The case study of one project: problem, own role, decisions with trade-offs, the Markdown body,
 * images and outcome (docs/decisions.md: projects are shown as case studies, not repositories).
 * Unknown and hidden projects render the not-found page with status 404.
 */
@Component({
  selector: 'app-case-study-page',
  imports: [NotFoundPage, RouterLink, UiProse, UnavailableNotice],
  templateUrl: './case-study-page.html',
  styleUrl: './case-study-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CaseStudyPage {
  private readonly content = inject(PublicContent);
  private readonly seo = inject(Seo);

  /** Bound from the route parameter by withComponentInputBinding. */
  readonly slug = input.required<string>();
  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  protected readonly strings = computed(() => uiStrings(this.locale()));
  protected readonly homePath = computed(() => (this.locale() === 'de' ? '/' : '/en'));

  private readonly studyResource = rxResource({
    params: () => ({ slug: this.slug(), locale: this.locale() }),
    stream: ({ params }) => this.content.caseStudy(params.slug, params.locale),
  });

  protected readonly failure = computed(() => failureOf(this.studyResource));
  protected readonly study = computed(() => valueOf(this.studyResource));
  protected readonly statusLine = computed(() => statusLine(this.study(), this.strings()));
  protected readonly stackLine = computed(() => (this.study()?.stackTags ?? []).join(' · '));
  protected readonly links = computed(() => studyLinks(this.study(), this.strings()));
  protected readonly decisions = computed(() => this.study()?.decisions ?? []);
  protected readonly images = computed(() =>
    (this.study()?.images ?? []).map((image) => ({
      url: documentUrl(image.documentId ?? 0),
      caption: image.caption ?? '',
    })),
  );

  constructor() {
    effect(() => {
      const study = this.study();
      if (study) {
        this.seo.apply({
          title: `${study.title} – ${this.strings().siteName}`,
          description: study.teaser ?? '',
          locale: this.locale(),
          paths: { de: `/projekte/${this.slug()}`, en: `/en/projects/${this.slug()}` },
        });
      }
    });
  }
}

function statusLine(study: PublicCaseStudy | undefined, strings: UiStrings): string {
  if (!study) {
    return '';
  }

  const status = strings.projects.statuses[study.status ?? ''] ?? '';
  const period = study.startedYear
    ? study.endedYear
      ? `${study.startedYear} – ${study.endedYear}`
      : `${strings.projects.since} ${study.startedYear}`
    : '';
  return [status, period].filter((part) => part.length > 0).join(' · ');
}

function studyLinks(study: PublicCaseStudy | undefined, strings: UiStrings): StudyLink[] {
  const links: StudyLink[] = [];
  if (study?.liveUrl) {
    links.push({ label: strings.projects.live, href: study.liveUrl });
  }
  if (study?.repositoryUrl) {
    links.push({ label: strings.projects.source, href: study.repositoryUrl });
  }
  return links;
}
