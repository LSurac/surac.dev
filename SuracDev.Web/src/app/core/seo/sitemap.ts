import { Locale } from '../locale/locale';
import { SITE_ORIGIN } from './seo';

/** A page in both languages; the sitemap links each to its counterpart. */
type PagePair = Readonly<Record<Locale, string>>;

const FIXED_PAGES: readonly PagePair[] = [
  { de: '/', en: '/en' },
  { de: '/ueber-mich', en: '/en/about' },
  { de: '/impressum', en: '/en/impressum' },
  { de: '/datenschutz', en: '/en/datenschutz' },
];

/**
 * The sitemap of the open site: start page, about page, legal pages and every visible case study, each in both
 * languages with `xhtml:link` alternates, as search engines expect for translated pages. Pure, so
 * `server.ts` only fetches the slugs and the content is tested here.
 */
export function buildSitemap(projectSlugs: readonly string[]): string {
  const pages: PagePair[] = [
    ...FIXED_PAGES,
    ...projectSlugs.map((slug) => ({ de: `/projekte/${slug}`, en: `/en/projects/${slug}` })),
  ];
  const urls = pages.flatMap((pair) => [entry(pair.de, pair), entry(pair.en, pair)]);
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">',
    ...urls,
    '</urlset>',
    '',
  ].join('\n');
}

function entry(path: string, pair: PagePair): string {
  return [
    '<url>',
    `<loc>${absolute(path)}</loc>`,
    `<xhtml:link rel="alternate" hreflang="de" href="${absolute(pair.de)}"/>`,
    `<xhtml:link rel="alternate" hreflang="en" href="${absolute(pair.en)}"/>`,
    '</url>',
  ].join('');
}

function absolute(path: string): string {
  return escapeXml(SITE_ORIGIN + path);
}

function escapeXml(text: string): string {
  return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
