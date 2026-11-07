import { FetchBackend, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable, of } from 'rxjs';
import { ApiBaseUrlBackend } from './api-base-url.backend';
import { API_BASE_URL } from './api-base-url.token';

describe('ApiBaseUrlBackend', () => {
  function setUp() {
    const handled: HttpRequest<unknown>[] = [];
    const fetchBackend = {
      handle: (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
        handled.push(request);
        return of(new HttpResponse({ body: new TextEncoder().encode('{"a":1}').buffer, status: 200 }));
      },
    };
    TestBed.configureTestingModule({
      providers: [
        ApiBaseUrlBackend,
        { provide: FetchBackend, useValue: fetchBackend },
        { provide: API_BASE_URL, useValue: 'http://api:8080' },
      ],
    });
    return { backend: TestBed.inject(ApiBaseUrlBackend), handled };
  }

  it('makes relative API URLs absolute for the server-side fetch', async () => {
    const { backend, handled } = setUp();

    await firstValueFrom(
      backend.handle(new HttpRequest('GET', '/api/public/profile', { responseType: 'json' })),
    );

    expect(handled[0]?.url).toBe('http://api:8080/api/public/profile');
    expect(handled[0]?.responseType).toBe('json');
  });

  it('sends API URLs that Angular already made absolute with the page origin to the API', async () => {
    const { backend, handled } = setUp();

    await firstValueFrom(
      backend.handle(
        new HttpRequest('GET', 'http://srv.example/api/public/profile?locale=de', { responseType: 'json' }),
      ),
    );
    await firstValueFrom(
      backend.handle(new HttpRequest('GET', 'https://other.example/feed.json', { responseType: 'json' })),
    );

    expect(handled[0]?.url).toBe('http://api:8080/api/public/profile?locale=de');
    expect(handled[1]?.url).toBe('https://other.example/feed.json');
  });

  it('fetches blob responses as ArrayBuffer so the transfer cache can serialise them', async () => {
    const { backend, handled } = setUp();

    const event = await firstValueFrom(
      backend.handle(new HttpRequest('GET', '/api/public/profile', { responseType: 'blob' })),
    );

    expect(handled[0]?.responseType).toBe('arraybuffer');
    expect((event as HttpResponse<ArrayBuffer>).body).toBeInstanceOf(ArrayBuffer);
  });
});
