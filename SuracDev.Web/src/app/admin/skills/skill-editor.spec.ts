import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminSkillGroup, AdminSkillGroupsClient } from '../../api/generated/admin.client';
import { SkillEditor } from './skill-editor';

const backend: AdminSkillGroup = {
  id: 1,
  sortOrder: 0,
  nameDe: 'Backend',
  nameEn: undefined,
  skills: [
    { name: 'C#', sinceYear: 2015, qualifierDe: undefined, qualifierEn: undefined },
    { name: 'ASP.NET', sinceYear: 2016, qualifierDe: 'Web-API', qualifierEn: 'Web API' },
    { name: 'EF Core', sinceYear: undefined, qualifierDe: undefined, qualifierEn: undefined },
  ],
};

const frontend: AdminSkillGroup = {
  id: 2,
  sortOrder: 1,
  nameDe: 'Frontend',
  nameEn: 'Frontend',
  skills: [{ name: 'Angular', sinceYear: 2019, qualifierDe: undefined, qualifierEn: undefined }],
};

function client(overrides: Partial<Record<'create' | 'update' | 'delete', ReturnType<typeof vi.fn>>> = {}) {
  return {
    list: vi.fn(() => of([structuredClone(backend), structuredClone(frontend)])),
    create: overrides.create ?? vi.fn((form) => of({ ...form, id: 3 })),
    update: overrides.update ?? vi.fn(() => of(undefined)),
    delete: overrides.delete ?? vi.fn(() => of(undefined)),
  };
}

async function renderEditor(groups = client()) {
  await render(SkillEditor, { providers: [{ provide: AdminSkillGroupsClient, useValue: groups }] });
  await screen.findByRole('list', { name: 'Kenntnisgruppen' });
  return groups;
}

function skillName(index: number) {
  return screen.getByLabelText('Kenntnis', { selector: `#skill-${index}-name` });
}

describe('admin skill editor', () => {
  it('lists the groups with the number of their skills', async () => {
    await renderEditor();

    expect(screen.getByRole('button', { name: /Backend.*3 Kenntnisse/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Frontend.*1 Kenntnis$/ })).toBeInTheDocument();
  });

  it('fills the form with the chosen group, both names and its skills', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Backend/ }));

    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Name')).toHaveValue('Backend');
    const english = screen.getByRole('group', { name: 'English' });
    expect(within(english).getByLabelText(/^Name/)).toHaveValue('');
    expect(within(english).getByText('Name').parentElement).toHaveTextContent('nicht übersetzt');
    expect(skillName(0)).toHaveValue('C#');
    expect(screen.getByLabelText('Seit', { selector: '#skill-1-since' })).toHaveValue(2016);
    expect(screen.getByLabelText('Seit', { selector: '#skill-2-since' })).toHaveValue(null);
    expect(screen.getByLabelText('Zusatz Deutsch', { selector: '#skill-1-qualifier-de' })).toHaveValue(
      'Web-API',
    );
    expect(screen.getByLabelText('Zusatz English', { selector: '#skill-1-qualifier-en' })).toHaveValue(
      'Web API',
    );
  });

  it('creates a new group with a skill and selects it', async () => {
    const groups = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neue Gruppe' }));
    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.type(within(german).getByLabelText('Name'), 'Werkzeuge');
    await userEvent.click(screen.getByRole('button', { name: 'Kenntnis hinzufügen' }));
    await userEvent.type(skillName(0), 'Docker');
    await userEvent.type(screen.getByLabelText('Seit', { selector: '#skill-0-since' }), '2020');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(groups.create).toHaveBeenCalledWith({
      sortOrder: 0,
      nameDe: 'Werkzeuge',
      nameEn: undefined,
      skills: [{ name: 'Docker', sinceYear: 2020, qualifierDe: undefined, qualifierEn: undefined }],
    });
    expect(groups.list).toHaveBeenCalledTimes(2);
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
  });

  it('replaces an existing group and sends an empty year as none', async () => {
    const groups = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Backend/ }));
    await userEvent.clear(screen.getByLabelText('Seit', { selector: '#skill-0-since' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(groups.update).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        nameDe: 'Backend',
        skills: [
          { name: 'C#', sinceYear: undefined, qualifierDe: undefined, qualifierEn: undefined },
          { name: 'ASP.NET', sinceYear: 2016, qualifierDe: 'Web-API', qualifierEn: 'Web API' },
          { name: 'EF Core', sinceYear: undefined, qualifierDe: undefined, qualifierEn: undefined },
        ],
      }),
    );
  });

  it('removes a skill', async () => {
    const groups = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Backend/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Entfernen: ASP.NET' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(groups.update).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        skills: [expect.objectContaining({ name: 'C#' }), expect.objectContaining({ name: 'EF Core' })],
      }),
    );
  });

  it('moves a skill up and down', async () => {
    const groups = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Backend/ }));
    expect(screen.getByRole('button', { name: 'Nach oben: C#' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Nach unten: EF Core' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Nach unten: C#' }));
    await userEvent.click(screen.getByRole('button', { name: 'Nach oben: EF Core' }));
    expect(skillName(1)).toHaveValue('EF Core');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(groups.update).toHaveBeenCalledWith(
      1,
      expect.objectContaining({
        skills: [
          expect.objectContaining({ name: 'ASP.NET', qualifierDe: 'Web-API' }),
          expect.objectContaining({ name: 'EF Core' }),
          expect.objectContaining({ name: 'C#', sinceYear: 2015 }),
        ],
      }),
    );
  });

  it('deletes a group only after confirmation', async () => {
    const groups = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Frontend/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    expect(screen.getByText('Diesen Eintrag wirklich löschen?')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Abbrechen' }));
    expect(groups.delete).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));
    expect(groups.delete).toHaveBeenCalledWith(2);
    expect(screen.queryByRole('button', { name: 'Kenntnis hinzufügen' })).not.toBeInTheDocument();
  });

  it('shows the errors of a rejected save in the summary and next to the name', async () => {
    const problem = {
      status: 400,
      errors: {
        'de.name': ['Required.'],
        skills: ["'C#' appears more than once."],
        'skills.sinceYear': ['Must be between 1950 and 2100.'],
        'skills.name': ['Too long.'],
      },
    };
    await renderEditor(client({ update: vi.fn(() => throwError(() => problem)) }));

    await userEvent.click(screen.getByRole('button', { name: /Backend/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    const alerts = await screen.findAllByRole('alert');
    expect(alerts[0]).toHaveTextContent('Required.');
    expect(alerts[0]).toHaveTextContent("'C#' appears more than once.");
    expect(alerts[0]).toHaveTextContent('Must be between 1950 and 2100.');
    expect(alerts[0]).toHaveTextContent('Too long.');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Name').parentElement).toHaveTextContent('Required.');
    expect(alerts[1]).toHaveTextContent("'C#' appears more than once.");
    expect(alerts[1]).toHaveTextContent('Must be between 1950 and 2100.');
    expect(alerts[1]).toHaveTextContent('Too long.');
  });
});
