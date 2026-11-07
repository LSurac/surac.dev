import { render, screen } from '@testing-library/angular';
import { UiErrorSummary } from './error-summary';

describe('ui-error-summary', () => {
  it('announces the messages as an alert', async () => {
    await render(UiErrorSummary, {
      inputs: { title: 'Nicht gespeichert', messages: ['Überschrift fehlt.', 'Datum ungültig.'] },
    });

    const alert = screen.getByRole('alert');

    expect(alert).toHaveTextContent('Nicht gespeichert');
    expect(screen.getAllByRole('listitem').map((item) => item.textContent?.trim())).toEqual([
      'Überschrift fehlt.',
      'Datum ungültig.',
    ]);
  });

  it('renders nothing without messages', async () => {
    await render(UiErrorSummary, { inputs: { title: 'Nicht gespeichert', messages: [] } });

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
