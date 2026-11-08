import { render, screen, within } from '@testing-library/angular';
import { of } from 'rxjs';
import { AdminApplicationStatisticsClient, ApplicationStatistics } from '../../api/generated/admin.client';
import { formatDuration, StatisticsPage } from './statistics-page';

const statistics: ApplicationStatistics = {
  sentCount: 4,
  respondedCount: 1,
  responseRate: 0.25,
  pipeline: [
    { status: 'Draft', count: 1 },
    { status: 'Sent', count: 3 },
    { status: 'Interview', count: 1 },
  ],
  applications: [
    {
      applicationId: 1,
      company: 'Beispiel AG',
      position: 'Fullstack-Entwickler',
      status: 'Interview',
      appliedOn: '2026-11-01',
      linkCount: 2,
      opens: 3,
      pdfDownloads: 1,
      documentDownloads: 2,
      firstOpenedAt: '2026-11-02T08:30:00Z',
      minutesToFirstOpen: 1500,
    },
    {
      applicationId: 2,
      company: 'Stille GmbH',
      position: 'Backend-Entwickler',
      status: 'Sent',
      appliedOn: undefined,
      linkCount: 0,
      opens: 0,
      pdfDownloads: 0,
      documentDownloads: 0,
      firstOpenedAt: undefined,
      minutesToFirstOpen: undefined,
    },
  ],
};

async function renderPage(value: ApplicationStatistics = statistics) {
  await render(StatisticsPage, {
    providers: [{ provide: AdminApplicationStatisticsClient, useValue: { get: vi.fn(() => of(value)) } }],
  });
  await screen.findByRole('heading', { level: 1, name: 'Statistik' });
}

describe('admin statistics page', () => {
  it('shows the response rate with sent and answered applications', async () => {
    await renderPage();

    expect(screen.getAllByRole('term').map((term) => term.textContent?.trim())).toEqual([
      'Antwortquote',
      'Versendet',
      'Antworten',
    ]);
    expect(screen.getByText('25 %')).toBeInTheDocument();
  });

  it('lists the status pipeline with a count per status', async () => {
    await renderPage();

    const pipeline = screen.getByRole('table', { name: 'Status' });

    expect(within(pipeline).getByRole('row', { name: 'Entwurf 1' })).toBeInTheDocument();
    expect(within(pipeline).getByRole('row', { name: 'Gespräch 1' })).toBeInTheDocument();
  });

  it('lists the link activity per application', async () => {
    await renderPage();

    const activity = screen.getByRole('table', { name: 'Link-Aktivität' });
    const rows = within(activity).getAllByRole('row');

    expect(rows[1]).toHaveTextContent('Beispiel AG');
    expect(rows[1]).toHaveTextContent('Gespräch');
    expect(rows[1]).toHaveTextContent('25 Std.');
    expect(rows[2]).toHaveTextContent('Stille GmbH');
    expect(rows[2]).toHaveTextContent('–');
  });

  it('shows no rate before anything was sent', async () => {
    await renderPage({ ...statistics, sentCount: 0, respondedCount: 0, responseRate: undefined });

    expect(screen.getAllByRole('definition')[0]).toHaveTextContent('–');
  });

  it('rounds the time to the first opening to minutes, hours or days', () => {
    expect(formatDuration(undefined)).toBe('–');
    expect(formatDuration(42)).toBe('42 Min.');
    expect(formatDuration(1500)).toBe('25 Std.');
    expect(formatDuration(4 * 24 * 60 + 5)).toBe('4 Tage');
  });
});
