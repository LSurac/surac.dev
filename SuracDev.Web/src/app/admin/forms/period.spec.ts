import { formatPeriod } from './period';

describe('formatPeriod', () => {
  it('shows month and year of both ends', () => {
    expect(formatPeriod('2019-01-01', '2021-12-31', 'heute')).toBe('01/2019 – 12/2021');
  });

  it('names an open end with the given word', () => {
    expect(formatPeriod('2022-09-01', undefined, 'heute')).toBe('09/2022 – heute');
  });

  it('stays empty without a start', () => {
    expect(formatPeriod(undefined, undefined, 'heute')).toBe('');
  });
});
