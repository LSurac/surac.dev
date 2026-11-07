import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { adminUnauthorizedInterceptor } from './admin-unauthorized.interceptor';

describe('adminUnauthorizedInterceptor', () => {
  function setUp() {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([adminUnauthorizedInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    return { http: TestBed.inject(HttpClient), backend: TestBed.inject(HttpTestingController), navigate };
  }

  it('sends an expired admin session back to the login', () => {
    const { http, backend, navigate } = setUp();

    http.get('/api/admin/profile').subscribe({ error: () => undefined });
    backend.expectOne('/api/admin/profile').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).toHaveBeenCalledWith('/admin/login');
  });

  it('leaves public requests and other errors alone', () => {
    const { http, backend, navigate } = setUp();

    http.get('/api/public/profile').subscribe({ error: () => undefined });
    http.get('/api/admin/projects').subscribe({ error: () => undefined });
    backend.expectOne('/api/public/profile').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/admin/projects').flush(null, { status: 409, statusText: 'Conflict' });

    expect(navigate).not.toHaveBeenCalled();
  });
});
