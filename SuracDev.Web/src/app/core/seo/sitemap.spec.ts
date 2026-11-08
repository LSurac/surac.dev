import { buildSitemap } from './sitemap';

describe('buildSitemap', () => {
  const xml = buildSitemap(['vereinswebsite', 'kassen-tool']);

  it('lists start page, about page, legal pages and every case study with absolute URLs', () => {
    expect(xml).toContain('<loc>https://surac.dev/</loc>');
    expect(xml).toContain('<loc>https://surac.dev/ueber-mich</loc>');
    expect(xml).toContain('<loc>https://surac.dev/en/about</loc>');
    expect(xml).toContain('<loc>https://surac.dev/en</loc>');
    expect(xml).toContain('<loc>https://surac.dev/impressum</loc>');
    expect(xml).toContain('<loc>https://surac.dev/en/datenschutz</loc>');
    expect(xml).toContain('<loc>https://surac.dev/projekte/vereinswebsite</loc>');
    expect(xml).toContain('<loc>https://surac.dev/en/projects/kassen-tool</loc>');
  });

  it('links each page to its other language version', () => {
    const entry = xml
      .split('<url>')
      .find((part) => part.includes('<loc>https://surac.dev/projekte/vereinswebsite</loc>'));

    expect(entry).toContain('hreflang="en" href="https://surac.dev/en/projects/vereinswebsite"');
    expect(entry).toContain('hreflang="de" href="https://surac.dev/projekte/vereinswebsite"');
  });

  it('is a well-formed urlset and escapes what XML needs escaped', () => {
    const odd = buildSitemap(['a&b']);

    expect(odd.startsWith('<?xml version="1.0" encoding="UTF-8"?>')).toBe(true);
    expect(odd).toContain('xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"');
    expect(odd).toContain('/projekte/a&amp;b');
    expect(odd).not.toContain('a&b<');
  });

  it('still lists the fixed pages without any project', () => {
    expect(buildSitemap([]).match(/<url>/g)).toHaveLength(8);
  });
});
