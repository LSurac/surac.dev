import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Meta } from '@angular/platform-browser';
import { PublicApplicationLink } from '../../api/generated/public.client';
import { Locale } from '../../core/locale/locale';
import { failureOf, valueOf } from '../content/load-state';
import { PublicContent } from '../content/public-content';
import { HomePage, LinkAudience } from '../home/home-page';
import { NotFoundPage } from '../not-found/not-found-page';
import { UnavailableNotice } from '../unavailable/unavailable-notice';

/**
 * An application link, `/b/<token>` and `/en/b/<token>`: the start page with the link's section
 * order, greeting and downloads (docs/content-model.md, "Application and ApplicationLink"). Unknown,
 * expired and revoked links render the not-found page with status 404, so a past link is not
 * confirmed. The page is never indexed; the token is the only thing that makes it reachable.
 */
@Component({
  selector: 'app-application-link-page',
  imports: [HomePage, NotFoundPage, UnavailableNotice],
  templateUrl: './application-link-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationLinkPage {
  private readonly content = inject(PublicContent);

  /** Bound from the route parameter by withComponentInputBinding. */
  readonly token = input.required<string>();
  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  private readonly linkResource = rxResource({
    params: () => ({ token: this.token(), locale: this.locale() }),
    stream: ({ params }) => this.content.applicationLink(params.token, params.locale),
  });

  protected readonly failure = computed(() => failureOf(this.linkResource));
  protected readonly audience = computed(() => {
    const link = valueOf(this.linkResource);
    return link ? toAudience(link, this.token(), this.locale()) : null;
  });

  constructor() {
    const meta = inject(Meta);
    meta.updateTag({ name: 'robots', content: 'noindex' });
    inject(DestroyRef).onDestroy(() => meta.removeTag('name="robots"'));
  }
}

function toAudience(link: PublicApplicationLink, token: string, locale: Locale): LinkAudience {
  const base = `/api/links/${encodeURIComponent(token)}`;
  return {
    sections: link.sections ?? [],
    greeting: link.greeting ?? null,
    cvHref: `${base}/cv.pdf?locale=${locale}`,
    documents: (link.documents ?? []).map((document) => ({
      label: document.label ?? document.fileName ?? '',
      href: `${base}/documents/${document.id}?locale=${locale}`,
    })),
  };
}
