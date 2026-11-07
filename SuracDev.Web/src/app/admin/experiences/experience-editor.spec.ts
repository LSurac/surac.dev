import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminExperience, AdminExperiencesClient } from '../../api/generated/admin.client';
import { ExperienceEditor } from './experience-editor';

const current: AdminExperience = {
  id: 1,
  company: 'Beispiel GmbH',
  location: 'Musterstadt',
  startDate: '2022-09-01',
  endDate: undefined,
  sortOrder: 0,
  isVisible: true,
  de: { roleTitle: 'Entwickler', summary: 'Text.', bullets: ['Eins', 'Zwei'], stackLine: '.NET' },
  en: undefined,
};

const hidden: AdminExperience = {
  ...current,
  id: 2,
  company: 'Versteckt AG',
  startDate: '2019-01-01',
  endDate: '2021-12-31',
  isVisible: false,
};

function client(overrides: Partial<Record<'create' | 'update' | 'delete', ReturnType<typeof vi.fn>>> = {}) {
  return {
    list: vi.fn(() => of([structuredClone(current), structuredClone(hidden)])),
    create: overrides.create ?? vi.fn((form) => of({ ...form, id: 3 })),
    update: overrides.update ?? vi.fn(() => of(undefined)),
    delete: overrides.delete ?? vi.fn(() => of(undefined)),
  };
}

async function renderEditor(experiences = client()) {
  await render(ExperienceEditor, { providers: [{ provide: AdminExperiencesClient, useValue: experiences }] });
  await screen.findByRole('list', { name: 'Berufsstationen' });
  return experiences;
}

describe('admin experience editor', () => {
  it('lists the stations with their period and marks hidden ones', async () => {
    await renderEditor();

    expect(screen.getByRole('button', { name: /Beispiel GmbH.*09\/2022 – heute/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Versteckt AG.*01\/2019 – 12\/2021/ })).toBeInTheDocument();
  });

  it('fills the form with the chosen station and both locales', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel GmbH/ }));

    expect(screen.getByLabelText('Firma')).toHaveValue('Beispiel GmbH');
    expect(screen.getByLabelText('Beginn')).toHaveValue('2022-09-01');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Stichpunkte')).toHaveValue('Eins\nZwei');
    const english = screen.getByRole('group', { name: 'English' });
    expect(within(english).getByText('Rolle').parentElement).toHaveTextContent('nicht übersetzt');
  });

  it('creates a new station and selects it', async () => {
    const experiences = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neue Station' }));
    await userEvent.type(screen.getByLabelText('Firma'), 'Neue AG');
    await userEvent.type(screen.getByLabelText('Beginn'), '2025-01-01');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.type(within(german).getByLabelText('Rolle'), 'Lead');
    await userEvent.type(within(german).getByLabelText('Stichpunkte'), 'A{enter}B');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(experiences.create).toHaveBeenCalledWith(
      expect.objectContaining({
        company: 'Neue AG',
        startDate: '2025-01-01',
        endDate: undefined,
        isVisible: true,
        de: expect.objectContaining({ roleTitle: 'Lead', bullets: ['A', 'B'] }),
      }),
    );
    expect(experiences.list).toHaveBeenCalledTimes(2);
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
  });

  it('replaces an existing station', async () => {
    const experiences = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Versteckt AG/ }));
    await userEvent.click(screen.getByLabelText('Auf der Website sichtbar'));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(experiences.update).toHaveBeenCalledWith(
      2,
      expect.objectContaining({ company: 'Versteckt AG', endDate: '2021-12-31', isVisible: true }),
    );
  });

  it('deletes a station only after confirmation', async () => {
    const experiences = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel GmbH/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    expect(screen.getByText('Diesen Eintrag wirklich löschen?')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Abbrechen' }));
    expect(experiences.delete).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));
    expect(experiences.delete).toHaveBeenCalledWith(1);
    expect(screen.queryByLabelText('Firma')).not.toBeInTheDocument();
  });

  it('shows the errors of a rejected save', async () => {
    const problem = { status: 400, errors: { endDate: ['Must not be before the start date.'] } };
    await renderEditor(client({ update: vi.fn(() => throwError(() => problem)) }));

    await userEvent.click(screen.getByRole('button', { name: /Beispiel GmbH/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Must not be before the start date.');
    expect(screen.getByLabelText('Ende').parentElement).toHaveTextContent(
      'Must not be before the start date.',
    );
  });
});
