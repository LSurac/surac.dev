import { ApiException } from '../../api/generated/admin.client';

/** What went wrong with a request, ready for the error summary and the field errors. */
export interface SaveProblem {
  readonly status: number | null;
  readonly messages: readonly string[];
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

interface ProblemBody {
  readonly status?: number;
  readonly title?: string;
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}

/**
 * Maps whatever a generated client threw to a {@link SaveProblem}: a parsed ProblemDetails (for
 * documented status codes), an ApiException whose body may still be ProblemDetails, or anything
 * else. Field errors keep the API's keys (`de.headline`, `facts`), which the editors use as control
 * keys. Unknown errors show `fallback`, never internals.
 */
export function toProblem(error: unknown, fallback: string): SaveProblem {
  if (error instanceof ApiException) {
    const body = parseBody(error.response);
    return body
      ? fromBody(body, error.status, fallback)
      : { status: error.status, messages: [fallback], fieldErrors: {} };
  }

  if (isProblemBody(error)) {
    return fromBody(error, error.status ?? null, fallback);
  }

  return { status: null, messages: [fallback], fieldErrors: {} };
}

function fromBody(body: ProblemBody, status: number | null, fallback: string): SaveProblem {
  const fieldErrors = body.errors ?? {};
  const fieldMessages = Object.values(fieldErrors).flat();
  const message = body.detail ?? (fieldMessages.length === 0 ? (body.title ?? fallback) : null);
  return {
    status: body.status ?? status,
    messages: message ? [message] : fieldMessages,
    fieldErrors,
  };
}

function parseBody(response: string): ProblemBody | null {
  try {
    const parsed: unknown = JSON.parse(response);
    return isProblemBody(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function isProblemBody(value: unknown): value is ProblemBody {
  return (
    typeof value === 'object' &&
    value !== null &&
    ('errors' in value || 'title' in value || 'detail' in value)
  );
}
