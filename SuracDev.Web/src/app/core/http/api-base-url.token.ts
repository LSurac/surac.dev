import { InjectionToken } from '@angular/core';

/** Empty in the browser (relative URLs through nginx), the API origin during server rendering. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});
