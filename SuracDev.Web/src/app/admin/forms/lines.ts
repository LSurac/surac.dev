/**
 * Ordered short lists (bullets, stack tags, address lines) are edited as one textarea with one
 * entry per line: simpler to type and reorder than a list of inputs.
 */
export function linesToText(lines: readonly string[] | null | undefined): string {
  return (lines ?? []).join('\n');
}

/** Splits a textarea into its non-empty, trimmed lines. */
export function textToLines(text: string | null | undefined): string[] {
  return (text ?? '')
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
}

/** True when the German text exists and the English one does not: the field falls back on the site. */
export function isUntranslated(
  german: string | null | undefined,
  english: string | null | undefined,
): boolean {
  return !!german?.trim() && !english?.trim();
}
