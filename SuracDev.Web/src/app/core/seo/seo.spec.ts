import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Seo } from './seo';

describe('Seo', () => {
  function apply(locale: 'de' | 'en') {
    TestBed.configureTestingModule({});
    const seo = TestBed.inject(Seo);
    seo.apply({
      title: 'Leonard Surac – Fullstack-Entwickler',
      description: 'Kurzprofil.',
      locale,
      paths: { de: '/projekte/beispiel', en: '/en/projects/beispiel' },
    });
    return TestBed.inject(DOCUMENT);
  }

  function link(document: Document, selector: string): string | null {
    return document.head.querySelector(selector)?.getAttribute('href') ?? null;
  }

  it('sets title, description and the document language', () => {
    const document = apply('de');

    expect(document.title).toBe('Leonard Surac – Fullstack-Entwickler');
    expect(document.head.querySelector('meta[name="description"]')?.getAttribute('content')).toBe(
      'Kurzprofil.',
    );
    expect(document.documentElement.lang).toBe('de');
  });

  it('links both language versions, German as x-default, and the canonical of the current one', () => {
    const document = apply('en');

    expect(link(document, 'link[rel="alternate"][hreflang="de"]')).toBe(
      'https://surac.dev/projekte/beispiel',
    );
    expect(link(document, 'link[rel="alternate"][hreflang="en"]')).toBe(
      'https://surac.dev/en/projects/beispiel',
    );
    expect(link(document, 'link[rel="alternate"][hreflang="x-default"]')).toBe(
      'https://surac.dev/projekte/beispiel',
    );
    expect(link(document, 'link[rel="canonical"]')).toBe('https://surac.dev/en/projects/beispiel');
    expect(document.documentElement.lang).toBe('en');
  });

  it('updates the existing tags instead of adding new ones on every navigation', () => {
    const document = apply('de');
    TestBed.inject(Seo).apply({
      title: 'Zweite Seite',
      description: 'Zwei.',
      locale: 'de',
      paths: { de: '/', en: '/en' },
    });

    expect(document.head.querySelectorAll('link[rel="canonical"]')).toHaveLength(1);
    expect(document.head.querySelectorAll('link[hreflang="en"]')).toHaveLength(1);
    expect(link(document, 'link[rel="canonical"]')).toBe('https://surac.dev/');
  });

  it('describes the page for link previews with Open Graph', () => {
    TestBed.configureTestingModule({});
    TestBed.inject(Seo).apply({
      title: 'Beispielprojekt – Leonard Surac',
      description: 'Ein Beispiel.',
      locale: 'en',
      paths: { de: '/projekte/beispiel', en: '/en/projects/beispiel' },
      type: 'article',
      imagePath: '/api/public/documents/21',
    });
    const document = TestBed.inject(DOCUMENT);
    const og = (property: string) =>
      document.head.querySelector(`meta[property="og:${property}"]`)?.getAttribute('content');

    expect(og('title')).toBe('Beispielprojekt – Leonard Surac');
    expect(og('description')).toBe('Ein Beispiel.');
    expect(og('url')).toBe('https://surac.dev/en/projects/beispiel');
    expect(og('type')).toBe('article');
    expect(og('locale')).toBe('en_GB');
    expect(og('image')).toBe('https://surac.dev/api/public/documents/21');
  });

  it('shows the site preview card when the page has no image of its own', () => {
    TestBed.configureTestingModule({});
    const seo = TestBed.inject(Seo);
    const paths = { de: '/', en: '/en' };
    seo.apply({ title: 'A', description: 'a', locale: 'de', paths, imagePath: '/api/public/documents/7' });
    seo.apply({ title: 'B', description: 'b', locale: 'de', paths });

    const document = TestBed.inject(DOCUMENT);
    const meta = (selector: string) => document.head.querySelector(selector)?.getAttribute('content');
    expect(meta('meta[property="og:image"]')).toBe('https://surac.dev/og-image.png');
    expect(meta('meta[name="twitter:card"]')).toBe('summary_large_image');
    expect(meta('meta[property="og:type"]')).toBe('website');
    expect(meta('meta[property="og:locale"]')).toBe('de_DE');
  });

  it('describes the page to search engines as one JSON-LD block that cannot close its script', () => {
    TestBed.configureTestingModule({});
    const paths = { de: '/', en: '/en' };
    TestBed.inject(Seo).apply({
      title: 'A',
      description: 'a',
      locale: 'de',
      paths,
      structuredData: { '@type': 'Person', name: '</script><b>Name' },
    });
    TestBed.inject(Seo).apply({
      title: 'A',
      description: 'a',
      locale: 'de',
      paths,
      structuredData: { '@type': 'Person', name: '</script><b>Name' },
    });

    const blocks = TestBed.inject(DOCUMENT).head.querySelectorAll('script[type="application/ld+json"]');
    expect(blocks).toHaveLength(1);
    expect(blocks[0].textContent).not.toContain('</script>');
    expect(JSON.parse(blocks[0].textContent ?? '')).toEqual({
      '@type': 'Person',
      name: '</script><b>Name',
    });
  });

  it('removes the structured data of the previous page when the next one has none', () => {
    TestBed.configureTestingModule({});
    const seo = TestBed.inject(Seo);
    const paths = { de: '/', en: '/en' };
    seo.apply({ title: 'A', description: 'a', locale: 'de', paths, structuredData: { '@type': 'Person' } });
    seo.apply({ title: 'B', description: 'b', locale: 'de', paths });

    expect(TestBed.inject(DOCUMENT).head.querySelector('script[type="application/ld+json"]')).toBeNull();
  });
});
