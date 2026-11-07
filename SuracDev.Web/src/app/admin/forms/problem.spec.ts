import { ApiException } from '../../api/generated/admin.client';
import { toProblem } from './problem';

describe('toProblem', () => {
  it('turns validation errors into field errors and summary messages', () => {
    const problem = toProblem(
      {
        status: 400,
        title: 'Validation failed.',
        errors: { 'de.headline': ['Required.'], facts: ['At most 4 facts.'] },
      },
      'Fehler',
    );

    expect(problem.status).toBe(400);
    expect(problem.fieldErrors['de.headline']).toEqual(['Required.']);
    expect(problem.messages).toEqual(['Required.', 'At most 4 facts.']);
  });

  it('uses the detail of a plain problem as the message', () => {
    const problem = toProblem({ status: 409, title: 'Conflict.', detail: 'The slug is taken.' }, 'Fehler');

    expect(problem.status).toBe(409);
    expect(problem.messages).toEqual(['The slug is taken.']);
    expect(problem.fieldErrors).toEqual({});
  });

  it('reads a problem from the body of an api exception', () => {
    const body = JSON.stringify({ status: 400, errors: { password: ['Wrong password.'] } });

    const problem = toProblem(new ApiException('Bad request', 400, body, {}, null), 'Fehler');

    expect(problem.fieldErrors['password']).toEqual(['Wrong password.']);
  });

  it('falls back to the given message for anything else', () => {
    expect(toProblem(new ApiException('Too many', 429, '', {}, null), 'Fehler')).toEqual({
      status: 429,
      messages: ['Fehler'],
      fieldErrors: {},
    });
    expect(toProblem(new Error('offline'), 'Fehler').messages).toEqual(['Fehler']);
  });
});
