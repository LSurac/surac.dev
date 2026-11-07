/**
 * What `src/server.ts` hands to every server render through REQUEST_CONTEXT. Keeps `process.env`
 * out of the application code: the app only ever sees this object.
 */
export interface ServerRequestContext {
  /** Absolute origin of the API, because server-side fetch cannot resolve relative `/api/...` URLs. */
  readonly apiBaseUrl: string;
}
