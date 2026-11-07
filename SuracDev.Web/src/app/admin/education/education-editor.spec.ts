import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminEducation, AdminEducationClient } from '../../api/generated/admin.client';
import { EducationEditor } from './education-editor';

const current: AdminEducation = {
  id: 1,
  institution: 'Beispiel-Hochschule',
  location: 'Musterstadt',
  startDate: '2022-09-01',
  endDate: undefined,
  grade: '1,8',
  sortOrder: 0,
  isVisible: true,
  de: { degree: 'B. Sc. Informatik', summary: 'Text.' },
  en: undefined,
};

const hidden: AdminEducation = {
  ...current,
  id: 2,
  institution: 'Versteckte Schule',
  startDate: '2019-01-01',
  endDate: '2021-12-31',
  grade: undefined,
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

async function renderEditor(education = client()) {
  await render(EducationEditor, { providers: [{ provide: AdminEducationClient, useValue: education }] });
  await screen.findByRole('list', { name: 'Ausbildungsstationen' });
  return education;
}

describe('admin education editor', () => {
  it('lists the entries with their period and marks hidden ones', async () => {
    await renderEditor();

    expect(screen.getByRole('button', { name: /Beispiel-Hochschule.*09\/2022 – heute/ })).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Versteckte Schule.*01\/2019 – 12\/2021/ }),
    ).toBeInTheDocument();
  });

  it('fills the form with the chosen entry and both locales', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel-Hochschule/ }));

    expect(screen.getByLabelText('Institution')).toHaveValue('Beispiel-Hochschule');
    expect(screen.getByLabelText('Beginn')).toHaveValue('2022-09-01');
    expect(screen.getByLabelText('Note')).toHaveValue('1,8');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Abschluss')).toHaveValue('B. Sc. Informatik');
    const english = screen.getByRole('group', { name: 'English' });
    expect(within(english).getByText('Abschluss').parentElement).toHaveTextContent('nicht übersetzt');
  });

  it('creates a new entry and selects it', async () => {
    const education = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neuer Eintrag' }));
    await userEvent.type(screen.getByLabelText('Institution'), 'Neue Schule');
    await userEvent.type(screen.getByLabelText('Beginn'), '2025-01-01');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.type(within(german).getByLabelText('Abschluss'), 'Abitur');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(education.create).toHaveBeenCalledWith(
      expect.objectContaining({
        institution: 'Neue Schule',
        startDate: '2025-01-01',
        endDate: undefined,
        isVisible: true,
        de: expect.objectContaining({ degree: 'Abitur' }),
      }),
    );
    expect(education.list).toHaveBeenCalledTimes(2);
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
  });

  it('replaces an existing entry', async () => {
    const education = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Versteckte Schule/ }));
    await userEvent.click(screen.getByLabelText('Auf der Website sichtbar'));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(education.update).toHaveBeenCalledWith(
      2,
      expect.objectContaining({ institution: 'Versteckte Schule', endDate: '2021-12-31', isVisible: true }),
    );
  });

  it('deletes an entry only after confirmation', async () => {
    const education = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel-Hochschule/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    expect(screen.getByText('Diesen Eintrag wirklich löschen?')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Abbrechen' }));
    expect(education.delete).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));
    expect(education.delete).toHaveBeenCalledWith(1);
    expect(screen.queryByLabelText('Institution')).not.toBeInTheDocument();
  });

  it('shows the errors of a rejected save', async () => {
    const problem = { status: 400, errors: { endDate: ['Must not be before the start date.'] } };
    await renderEditor(client({ update: vi.fn(() => throwError(() => problem)) }));

    await userEvent.click(screen.getByRole('button', { name: /Beispiel-Hochschule/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Must not be before the start date.');
    expect(screen.getByLabelText('Ende').parentElement).toHaveTextContent(
      'Must not be before the start date.',
    );
  });
});
