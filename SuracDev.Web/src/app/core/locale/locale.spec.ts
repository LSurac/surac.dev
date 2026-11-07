import { isLocale, localeFromUrl, switchLocaleUrl } from './locale';

describe('locale', () => {
  it('reads the locale from the first URL segment and defaults to German', () => {
    expect(localeFromUrl('/')).toBe('de');
    expect(localeFromUrl('/projekte/sk-sulzfeld')).toBe('de');
    expect(localeFromUrl('/en')).toBe('en');
    expect(localeFromUrl('/en/projects/sk-sulzfeld?x=1#top')).toBe('en');
    expect(localeFromUrl('/english')).toBe('de');
  });

  it('recognises only the supported locales', () => {
    expect(isLocale('de')).toBe(true);
    expect(isLocale('en')).toBe(true);
    expect(isLocale('fr')).toBe(false);
    expect(isLocale(undefined)).toBe(false);
  });

  it('switches the locale prefix and keeps path, query and hash', () => {
    expect(switchLocaleUrl('/', 'en')).toBe('/en');
    expect(switchLocaleUrl('/en', 'de')).toBe('/');
    expect(switchLocaleUrl('/projekte/x?view=1#top', 'en')).toBe('/en/projekte/x?view=1#top');
    expect(switchLocaleUrl('/en/projects/x', 'de')).toBe('/projects/x');
    expect(switchLocaleUrl('/en', 'en')).toBe('/en');
  });
});
