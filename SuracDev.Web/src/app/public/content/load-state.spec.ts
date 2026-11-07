import { signal } from '@angular/core';
import { Resource } from '@angular/core';
import { failureOf, valueOf } from './load-state';

function fakeResource(state: { value?: string; error?: Error }): Resource<string | undefined> {
  return {
    value: signal(state.value),
    error: signal(state.error),
    hasValue: () => state.value !== undefined && !state.error,
  } as unknown as Resource<string | undefined>;
}

describe('load state', () => {
  it('reads the value only when there is one', () => {
    expect(valueOf(fakeResource({ value: 'x' }))).toBe('x');
    expect(valueOf(fakeResource({ error: new Error('boom') }))).toBeUndefined();
  });

  it('tells a missing resource from a failing API', () => {
    expect(failureOf(fakeResource({ value: 'x' }))).toBeNull();
    expect(failureOf(fakeResource({ error: new Error('wrapped', { cause: { status: 404 } }) }))).toBe(
      'not-found',
    );
    expect(failureOf(fakeResource({ error: Object.assign(new Error('direct'), { status: 404 }) }))).toBe(
      'not-found',
    );
    expect(failureOf(fakeResource({ error: new Error('wrapped', { cause: { status: 500 } }) }))).toBe(
      'unavailable',
    );
    expect(failureOf(fakeResource({ error: new Error('offline') }))).toBe('unavailable');
  });
});
