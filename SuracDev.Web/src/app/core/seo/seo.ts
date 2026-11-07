import { DOCUMENT, inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { Locale } from '../locale/locale';

/** The public origin of the site; canonical and hreflang links must be absolute. */
export const SITE_ORIGIN = 'https://surac.dev';

/** What one public page tells search engines. `paths` are the page's paths in both locales. */
export interface PageMeta {
  readonly title: string;
  readonly description: string;
  readonly locale: Locale;
  readonly paths: Readonly<Record<Locale, string>>;
}

/**
 * Title, description, document language, canonical and hreflang links of the current page. Runs
 * on the server, so the first HTML a crawler sees is complete; tags are updated in place, never
 * duplicated on client-side navigation.
 */
@Injectable({ providedIn: 'root' })
export class Seo {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  apply(page: PageMeta): void {
    this.title.setTitle(page.title);
    this.meta.updateTag({ name: 'description', content: page.description });
    this.document.documentElement.lang = page.locale;
    this.setLink({ rel: 'canonical' }, page.paths[page.locale]);
    this.setLink({ rel: 'alternate', hreflang: 'de' }, page.paths.de);
    this.setLink({ rel: 'alternate', hreflang: 'en' }, page.paths.en);
    this.setLink({ rel: 'alternate', hreflang: 'x-default' }, page.paths.de);
  }

  private setLink(attributes: { rel: string; hreflang?: string }, path: string): void {
    const selector = attributes.hreflang
      ? `link[rel="${attributes.rel}"][hreflang="${attributes.hreflang}"]`
      : `link[rel="${attributes.rel}"]`;
    let link = this.document.head.querySelector<HTMLLinkElement>(selector);
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', attributes.rel);
      if (attributes.hreflang) {
        link.setAttribute('hreflang', attributes.hreflang);
      }
      this.document.head.appendChild(link);
    }

    link.setAttribute('href', SITE_ORIGIN + path);
  }
}
