import { render, screen, within } from '@testing-library/angular';
import { UiTimeline } from './timeline';

describe('ui-timeline', () => {
  it('lists entries with period, title, organisation, bullets and stack line', async () => {
    await render(UiTimeline, {
      inputs: {
        label: 'Berufsstationen',
        entries: [
          {
            id: 1,
            period: '09/2022 – heute',
            title: 'Softwareentwickler',
            organisation: 'Beispiel GmbH',
            location: 'Musterstadt',
            summary: 'Zusammenfassung.',
            bullets: ['Erster Punkt', 'Zweiter Punkt'],
            detail: '.NET · Angular',
          },
          { id: 2, period: '2019 – 2022', title: 'Ausbildung', organisation: 'Schule', bullets: [] },
        ],
      },
    });

    const entries = within(screen.getByRole('list', { name: 'Berufsstationen' })).getAllByRole('listitem', {
      name: undefined,
    });
    const first = screen.getByRole('heading', { level: 3, name: 'Softwareentwickler' }).closest('article')!;

    expect(entries.length).toBeGreaterThanOrEqual(2);
    expect(within(first).getByText('09/2022 – heute')).toBeInTheDocument();
    expect(within(first).getByText(/Beispiel GmbH/)).toBeInTheDocument();
    expect(within(first).getByText('Zweiter Punkt')).toBeInTheDocument();
    expect(within(first).getByText('.NET · Angular')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 3, name: 'Ausbildung' })).toBeInTheDocument();
  });
});
