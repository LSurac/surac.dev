import { FetchBackend, HttpBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';

/**
 * Server-only HTTP backend that sends every `/api/...` request to the API container. Angular's
 * server rendering already turns relative URLs into absolute ones with the page's own origin
 * (`http://<public host>/api/...`), which from inside the container points at nothing, so the
 * path of any `/api/` URL is re-based onto API_BASE_URL. It sits below the interceptors on purpose:
 * the hydration transfer cache keys responses by the URL the browser requests.
 *
 * It also fetches `blob` responses (every NSwag call) as ArrayBuffer: the transfer cache serialises
 * blob bodies with `new Uint8Array(body)`, which yields nothing for a Blob, so the browser would
 * hydrate with empty responses. The cache restores the bytes as a Blob in the browser, and the
 * server's FileReader shim reads the ArrayBuffer (docs/gotchas.md).
 */
@Injectable()
export class ApiBaseUrlBackend implements HttpBackend {
  private readonly fetchBackend = inject(FetchBackend);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    const url = toApiUrl(request.url, this.apiBaseUrl);
    const responseType = request.responseType === 'blob' ? 'arraybuffer' : request.responseType;
    return this.fetchBackend.handle(request.clone({ url, responseType }));
  }
}

const API_PATH = '/api/';

/** `/api/x?y` and `http://any-host/api/x?y` both become `<apiBaseUrl>/api/x?y`; other URLs stay. */
function toApiUrl(url: string, apiBaseUrl: string): string {
  if (url.startsWith('/')) {
    return apiBaseUrl + url;
  }

  const parsed = URL.canParse(url) ? new URL(url) : null;
  return parsed?.pathname.startsWith(API_PATH) ? apiBaseUrl + parsed.pathname + parsed.search : url;
}
