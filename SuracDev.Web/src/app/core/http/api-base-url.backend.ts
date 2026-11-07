import { FetchBackend, HttpBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';

/**
 * Server-only HTTP backend that makes relative `/api/...` requests absolute. It sits below the
 * interceptors on purpose: the hydration transfer cache keys responses by the request URL, and the
 * browser requests the relative URL, so the cache only matches when the server cached it under the
 * same relative key.
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
    const url = request.url.startsWith('/') ? this.apiBaseUrl + request.url : request.url;
    const responseType = request.responseType === 'blob' ? 'arraybuffer' : request.responseType;
    return this.fetchBackend.handle(request.clone({ url, responseType }));
  }
}
