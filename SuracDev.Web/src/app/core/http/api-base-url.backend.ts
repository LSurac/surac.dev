import { FetchBackend, HttpBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';

/**
 * Server-only HTTP backend that makes relative `/api/...` requests absolute. It sits below the
 * interceptors on purpose: the hydration transfer cache keys responses by the request URL, and the
 * browser requests the relative URL, so the cache only matches when the server cached it under the
 * same relative key.
 */
@Injectable()
export class ApiBaseUrlBackend implements HttpBackend {
  private readonly fetchBackend = inject(FetchBackend);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    const absolute = request.url.startsWith('/')
      ? request.clone({ url: this.apiBaseUrl + request.url })
      : request;
    return this.fetchBackend.handle(absolute);
  }
}
