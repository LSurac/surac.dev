import { Resource } from '@angular/core';

const NOT_FOUND = 404;

/** Why a page's content is missing: the API said it does not exist, or the API failed. */
export type LoadFailure = 'not-found' | 'unavailable';

/** The resource's value, or undefined while loading or after an error (reading `value()` then throws). */
export function valueOf<T>(resource: Resource<T | undefined>): T | undefined {
  return resource.hasValue() ? resource.value() : undefined;
}

/** Classifies a failed resource; null while it loads or succeeded. */
export function failureOf(resource: Resource<unknown>): LoadFailure | null {
  const error = resource.error();
  if (!error) {
    return null;
  }

  return statusOf(error) === NOT_FOUND || statusOf(error.cause) === NOT_FOUND ? 'not-found' : 'unavailable';
}

function statusOf(value: unknown): number | null {
  return typeof value === 'object' && value !== null && 'status' in value && typeof value.status === 'number'
    ? value.status
    : null;
}
