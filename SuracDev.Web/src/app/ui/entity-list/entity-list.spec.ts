import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { UiEntityList } from './entity-list';

describe('ui-entity-list', () => {
  const items = [
    { id: 1, title: 'Beispiel GmbH', meta: 'seit 2022' },
    { id: 2, title: 'Versteckt AG', meta: '2019 – 2021', muted: true },
  ];

  it('lists the entries and marks the selected one', async () => {
    await render(UiEntityList, {
      inputs: { items, selectedId: 2, label: 'Berufsstationen', createLabel: 'Neue Station' },
    });

    expect(screen.getByRole('list', { name: 'Berufsstationen' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Versteckt AG/ })).toHaveAttribute('aria-current', 'true');
    expect(screen.getByRole('button', { name: /Beispiel GmbH/ })).not.toHaveAttribute('aria-current');
  });

  it('emits the chosen id and the wish to create a new entry', async () => {
    const selected = vi.fn();
    const created = vi.fn();
    await render(UiEntityList, {
      inputs: { items, selectedId: null, label: 'Berufsstationen', createLabel: 'Neue Station' },
      on: { selected, created },
    });

    await userEvent.click(screen.getByRole('button', { name: /Beispiel GmbH/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Neue Station' }));

    expect(selected).toHaveBeenCalledWith(1);
    expect(created).toHaveBeenCalledTimes(1);
  });
});
