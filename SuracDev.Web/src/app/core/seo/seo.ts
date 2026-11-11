import { DOCUMENT, inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { Locale } from '../locale/locale';

/** The public origin of the site; canonical and hreflang links must be absolute. */
export const SITE_ORIGIN = 'https://surac.dev';

/** The preview card (name and title line, no photo) for pages without an image of their own. */
const DEFAULT_IMAGE_PATH = '/og-image.png';
const STRUCTURED_DATA_SELECTOR = 'script[type="application/ld+json"]';

/**
 * What one public page tells search engines and link previews. `paths` are the page's paths in
 * both locales; `imagePath` is a site path of a public image for the preview; `structuredData` is
 * the page's schema.org JSON-LD (structured-data.ts).
 */
export interface PageMeta {
  readonly title: string;
  readonly description: string;
  readonly locale: Locale;
  readonly paths: Readonly<Record<Locale, string>>;
  readonly type?: 'website' | 'article';
  readonly imagePath?: string | null;
  readonly structuredData?: Readonly<Record<string, unknown>> | null;
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
    this.applyStructuredData(page.structuredData);
  }

  // A data block, not a script: CSP does not apply to it. `<` is escaped so text from the content
  // can never close the element.
  private applyStructuredData(data: PageMeta['structuredData']): void {
    let block = this.document.head.querySelector<HTMLScriptElement>(STRUCTURED_DATA_SELECTOR);
    if (!data) {
      block?.remove();
      return;
    }

    if (!block) {
      block = this.document.createElement('script');
      block.setAttribute('type', 'application/ld+json');
      this.document.head.appendChild(block);
    }

    block.textContent = JSON.stringify(data).replace(/</g, '\\u003c');
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

    this.meta.updateTag({
      property: 'og:image',
      content: SITE_ORIGIN + (page.imagePath || DEFAULT_IMAGE_PATH),
    });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
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
