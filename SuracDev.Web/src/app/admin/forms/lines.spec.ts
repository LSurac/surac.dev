import { isUntranslated, linesToText, textToLines } from './lines';

describe('line lists', () => {
  it('round-trips a list through one line per entry', () => {
    expect(linesToText(['eins', 'zwei'])).toBe('eins\nzwei');
    expect(textToLines('  eins \n\n zwei\n')).toEqual(['eins', 'zwei']);
    expect(linesToText(undefined)).toBe('');
    expect(textToLines(null)).toEqual([]);
  });

  it('calls a field untranslated only when German has text and English has none', () => {
    expect(isUntranslated('Titel', '')).toBe(true);
    expect(isUntranslated('Titel', ' ')).toBe(true);
    expect(isUntranslated('Titel', 'Title')).toBe(false);
    expect(isUntranslated('', '')).toBe(false);
  });
});
