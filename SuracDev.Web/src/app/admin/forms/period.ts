/** "09/2022 – heute" for list entries of the career editors; dates come as ISO `yyyy-MM-dd`. */
export function formatPeriod(start: string | undefined, end: string | undefined, openEnd: string): string {
  if (!start) {
    return '';
  }

  return `${monthYear(start)} – ${end ? monthYear(end) : openEnd}`;
}

function monthYear(isoDate: string): string {
  const [year, month] = isoDate.split('-');
  return `${month}/${year}`;
}
