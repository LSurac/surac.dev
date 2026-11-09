import { Injectable } from '@angular/core';

const PAGE_VIEW_URL = '/api/public/page-views';

/**
 * The path a page view is reported under: no query string or fragment, and an application link as
 * `/b/{token}`. A token is a capability; it never leaves the browser in a page view (the API
 * replaces it again, docs/security-and-privacy.md).
 */
export function pageViewPath(url: string): string {
  const path = url.split(/[?#]/, 1)[0];
  return path.replace(/^\/b\/[^/]+/, '/b/{token}');
}

/**
 * Reports each page the browser shows to the API's usage log (PageViewsController), because after
 * the first page an Angular route change only fetches data and leaves no trace of which page was
 * opened. The first page of a visit also carries the host of the external page the visitor came from
 * (host only), which links the search data to what visitors do after landing.
 *
 * Deliberately not through HttpClient: a lost beacon must neither show an error nor delay a page, and
 * sendBeacon still delivers when the visitor leaves right after the navigation. An automated browser
 * (E2E, release checks) sends nothing: those are no visitors.
 */
@Injectable({ providedIn: 'root' })
export class PageViewTracker {
  private lastPath: string | null = null;

  track(path: string): void {
    if (navigator.webdriver || path === this.lastPath) {
      return;
    }
    const referrerHost = this.lastPath === null ? externalReferrerHost() : null;
    this.lastPath = path;

    const body = JSON.stringify(referrerHost ? { path, referrerHost } : { path });
    if (navigator.sendBeacon?.(PAGE_VIEW_URL, new Blob([body], { type: 'application/json' }))) {
      return;
    }

    void fetch(PAGE_VIEW_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body,
      keepalive: true,
      credentials: 'same-origin',
    }).catch(() => undefined);
  }
}

function externalReferrerHost(): string | null {
  try {
    const host = document.referrer ? new URL(document.referrer).hostname.toLowerCase() : '';
    return host && host !== location.hostname.toLowerCase() ? host : null;
  } catch {
    return null;
  }
}
