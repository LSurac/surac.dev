import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LocaleService } from './locale.service';

describe('LocaleService', () => {
  function setUp(browserPath: string) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Location, useValue: { path: () => browserPath } }],
    });
    return TestBed.inject(LocaleService);
  }

  it('takes the locale from the address before the first navigation has finished', () => {
    // During hydration the router still reports "/" for an English page; rendering German labels
    // until the navigation ends made the header wrap and shift the page (CLS 0.3 on production).
    const service = setUp('/en/impressum');

    expect(TestBed.inject(Router).url).toBe('/');
    expect(service.locale()).toBe('en');
    expect(service.url()).toBe('/en/impressum');
  });

  it('keeps German for German addresses', () => {
    expect(setUp('/impressum').locale()).toBe('de');
  });
});
