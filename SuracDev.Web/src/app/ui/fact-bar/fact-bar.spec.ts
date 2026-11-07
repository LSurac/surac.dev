import { render, screen } from '@testing-library/angular';
import { UiFactBar } from './fact-bar';

describe('ui-fact-bar', () => {
  it('renders every fact as a term/definition pair', async () => {
    await render(UiFactBar, {
      inputs: {
        facts: [
          { value: '5', label: 'Jahre Entwicklung im Produktivbetrieb' },
          { value: '1,8', label: 'IHK-Abschluss' },
        ],
      },
    });

    const terms = screen.getAllByRole('term');
    const definitions = screen.getAllByRole('definition');

    expect(terms.map((term) => term.textContent?.trim())).toEqual([
      'Jahre Entwicklung im Produktivbetrieb',
      'IHK-Abschluss',
    ]);
    expect(definitions.map((definition) => definition.textContent?.trim())).toEqual(['5', '1,8']);
  });

  it('renders nothing but an empty list when there are no facts', async () => {
    await render(UiFactBar, { inputs: { facts: [] } });

    expect(screen.queryAllByRole('term')).toHaveLength(0);
  });
});
