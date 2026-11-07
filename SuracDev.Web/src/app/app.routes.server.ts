import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Public pages render on the server; the admin area (phase 1) renders in the browser only. The
 * dev-only styleguide route is not listed: it does not exist in a production build, and the SSR
 * build refuses server routes that match no client route.
 */
export const serverRoutes: ServerRoute[] = [
  { path: 'admin/**', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Server },
];
