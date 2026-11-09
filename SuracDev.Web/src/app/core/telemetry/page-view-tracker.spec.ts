import { TestBed } from '@angular/core/testing';
import { PageViewTracker, pageViewPath } from './page-view-tracker';

describe('pageViewPath', () => {
  it.each([
    ['/en/projects/kassensturz', '/en/projects/kassensturz'],
    ['/projekte?featured=true#top', '/projekte'],
    ['/b/abc_DEF-123', '/b/{token}'],
    ['/b/abc_DEF-123?locale=en', '/b/{token}'],
    ['/admin/bewerbungen/7', '/admin/bewerbungen/7'],
  ])('reports %s as %s', (url, path) => {
    expect(pageViewPath(url)).toBe(path);
  });
});

describe('PageViewTracker', () => {
  const beacons: { url: string; body: unknown }[] = [];

  function setUp(options: { webdriver?: boolean; referrer?: string } = {}) {
    beacons.length = 0;
    vi.spyOn(navigator, 'webdriver', 'get').mockReturnValue(options.webdriver ?? false);
    vi.spyOn(document, 'referrer', 'get').mockReturnValue(options.referrer ?? '');
    Object.defineProperty(navigator, 'sendBeacon', {
      configurable: true,
      value: (url: string, blob: Blob) => {
        void blob.text().then((text) => beacons.push({ url, body: JSON.parse(text) }));
        return true;
      },
    });
    return TestBed.inject(PageViewTracker);
  }

  afterEach(() => {
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  it('sends each page once, with the external referrer host on the first page only', async () => {
    const tracker = setUp({ referrer: 'https://www.google.com/search?q=surac' });

    tracker.track('/');
    tracker.track('/');
    tracker.track('/projekte/kassensturz');
    await vi.waitFor(() => expect(beacons).toHaveLength(2));

    expect(beacons[0]).toEqual({
      url: '/api/public/page-views',
      body: { path: '/', referrerHost: 'www.google.com' },
    });
    expect(beacons[1]).toEqual({ url: '/api/public/page-views', body: { path: '/projekte/kassensturz' } });
  });

  it('leaves out a referrer from the site itself', async () => {
    const tracker = setUp({ referrer: `${location.origin}/impressum` });

    tracker.track('/');
    await vi.waitFor(() => expect(beacons).toHaveLength(1));

    expect(beacons[0].body).toEqual({ path: '/' });
  });

  it('sends nothing from an automated browser', async () => {
    const tracker = setUp({ webdriver: true });

    tracker.track('/');
    await new Promise((resolve) => setTimeout(resolve, 10));

    expect(beacons).toHaveLength(0);
  });
});
