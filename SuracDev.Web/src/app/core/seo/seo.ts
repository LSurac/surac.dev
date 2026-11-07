import { DOCUMENT, inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { Locale } from '../locale/locale';

/** The public origin of the site; canonical and hreflang links must be absolute. */
export const SITE_ORIGIN = 'https://surac.dev';

/**
 * What one public page tells search engines and link previews. `paths` are the page's paths in
 * both locales; `imagePath` is a site path of a public image for the preview.
 */
export interface PageMeta {
  readonly title: string;
  readonly description: string;
  readonly locale: Locale;
  readonly paths: Readonly<Record<Locale, string>>;
  readonly type?: 'website' | 'article';
  readonly imagePath?: string | null;
}

const OG_LOCALES: Readonly<Record<Locale, string>> = { de: 'de_DE', en: 'en_GB' };

/**
 * Title, description, document language, canonical and hreflang links and the Open Graph tags of
 * the current page (link previews in messengers and on LinkedIn). Runs
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
    this.applyOpenGraph(page);
  }

  private applyOpenGraph(page: PageMeta): void {
    const tags: Record<string, string> = {
      'og:title': page.title,
      'og:description': page.description,
      'og:url': SITE_ORIGIN + page.paths[page.locale],
      'og:type': page.type ?? 'website',
      'og:locale': OG_LOCALES[page.locale],
      'og:site_name': 'surac.dev',
    };
    for (const [property, content] of Object.entries(tags)) {
      this.meta.updateTag({ property, content });
    }

    if (page.imagePath) {
      this.meta.updateTag({ property: 'og:image', content: SITE_ORIGIN + page.imagePath });
    } else {
      this.meta.removeTag('property="og:image"');
    }
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
