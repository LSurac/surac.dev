import { render, screen, within } from '@testing-library/angular';
import { UiSkillColumns } from './skill-columns';

describe('ui-skill-columns', () => {
  it('lists every group with its skills, the since year and the qualifier, never a level', async () => {
    const { container } = await render(UiSkillColumns, {
      inputs: {
        sinceLabel: 'seit',
        groups: [
          {
            id: 1,
            name: 'Backend',
            skills: [
              { name: 'C#', sinceYear: 2019 },
              { name: 'VB.NET', sinceYear: 2019, qualifier: 'Legacy' },
            ],
          },
          { id: 2, name: 'Frontend', skills: [{ name: 'Angular' }] },
        ],
      },
    });

    const backend = screen.getByRole('list', { name: 'Backend' });

    expect(screen.getByRole('heading', { level: 3, name: 'Backend' })).toBeInTheDocument();
    expect(within(backend).getByText('C#').closest('li')).toHaveTextContent('seit 2019');
    expect(within(backend).getByText('VB.NET').closest('li')).toHaveTextContent('Legacy');
    expect(within(screen.getByRole('list', { name: 'Frontend' })).getByText('Angular')).toBeInTheDocument();
    expect(container.querySelector('progress, meter, [role="progressbar"]')).toBeNull();
  });
});
