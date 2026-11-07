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
});
