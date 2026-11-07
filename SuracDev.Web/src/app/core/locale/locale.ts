/** The two content languages. German is the primary locale and lives at the URL root. */
export type Locale = 'de' | 'en';

export const DEFAULT_LOCALE: Locale = 'de';
export const LOCALES: readonly Locale[] = ['de', 'en'];

export function isLocale(value: unknown): value is Locale {
  return LOCALES.includes(value as Locale);
}

/** `/en`, `/en/`, `/en/projects/x` are English; everything else is German. */
export function localeFromUrl(url: string): Locale {
  const firstSegment = url.split(/[?#]/)[0]?.split('/').filter(Boolean)[0];
  return isLocale(firstSegment) ? firstSegment : DEFAULT_LOCALE;
}

/** The same path in the other locale, so a locale switch keeps the visitor on the page. */
export function switchLocaleUrl(url: string, target: Locale): string {
  const [pathAndQuery = '/', hash] = url.split('#');
  const [path, query] = pathAndQuery.split('?');
  const segments = (path ?? '').split('/').filter(Boolean);
  if (isLocale(segments[0])) {
    segments.shift();
  }
  if (target !== DEFAULT_LOCALE) {
    segments.unshift(target);
  }
  const rebuilt = '/' + segments.join('/');
  return rebuilt + (query ? `?${query}` : '') + (hash ? `#${hash}` : '');
}
