import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminDocumentsClient, AdminProject, AdminProjectsClient } from '../../api/generated/admin.client';
import { ProjectEditor } from './project-editor';

const project: AdminProject = {
  id: 4,
  slug: 'beispielprojekt',
  status: 'Live',
  startedYear: 2024,
  endedYear: undefined,
  liveUrl: undefined,
  repositoryUrl: 'https://github.com/example/repo',
  stackTags: ['.NET', 'Angular'],
  sortOrder: 0,
  isFeatured: true,
  isVisible: true,
  de: {
    title: 'Beispielprojekt',
    teaser: 'Kurz.',
    problem: undefined,
    myRole: undefined,
    outcome: undefined,
    caseStudyMarkdown: '## Abschnitt',
  },
  en: undefined,
  decisions: [{ de: { title: 'Entscheidung', tradeOff: 'Abwägung' }, en: undefined }],
  images: [],
};

function clients(overrides: Partial<Record<'create' | 'update', ReturnType<typeof vi.fn>>> = {}) {
  return {
    projects: {
      list: vi.fn(() => of([structuredClone(project)])),
      create: overrides.create ?? vi.fn((form) => of({ ...form, id: 9 })),
      update: overrides.update ?? vi.fn(() => of(undefined)),
      delete: vi.fn(() => of(undefined)),
    },
    documents: {
      list: vi.fn(() =>
        of([
          {
            id: 21,
            fileName: 'start.webp',
            contentType: 'image/webp',
            visibility: 'Public',
            labelDe: 'Startseite',
          },
          { id: 22, fileName: 'cv.pdf', contentType: 'application/pdf', visibility: 'Public' },
        ]),
      ),
    },
  };
}

async function renderEditor(mocks = clients()) {
  await render(ProjectEditor, {
    providers: [
      { provide: AdminProjectsClient, useValue: mocks.projects },
      { provide: AdminDocumentsClient, useValue: mocks.documents },
    ],
  });
  await screen.findByRole('list', { name: 'Projekte' });
  return mocks;
}

describe('admin project editor', () => {
  it('lists the projects and fills the form with the chosen one', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispielprojekt/ }));

    expect(screen.getByLabelText('Slug')).toHaveValue('beispielprojekt');
    expect(screen.getByLabelText('Status')).toHaveDisplayValue('Live');
    expect(screen.getByLabelText('Stack')).toHaveValue('.NET\nAngular');
    expect(screen.getByLabelText('Auf der Startseite')).toBeChecked();
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Fallstudie (Markdown)')).toHaveValue('## Abschnitt');
    expect(screen.getByLabelText('Entscheidung (Deutsch)', { selector: '#decision-0-title-de' })).toHaveValue(
      'Entscheidung',
    );
  });

  it('creates a project with decisions and images from the public images', async () => {
    const mocks = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neues Projekt' }));
    await userEvent.type(screen.getByLabelText('Slug'), 'neues-projekt');
    await userEvent.selectOptions(screen.getByLabelText('Status'), 'Im Betrieb');
    await userEvent.type(screen.getByLabelText('Stack'), 'C#{enter}PostgreSQL');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.type(within(german).getByLabelText('Titel'), 'Neu');
    await userEvent.type(within(german).getByLabelText('Teaser'), 'Kurztext');
    await userEvent.click(screen.getByRole('button', { name: 'Entscheidung hinzufügen' }));
    await userEvent.type(
      screen.getByLabelText('Entscheidung (Deutsch)', { selector: '#decision-0-title-de' }),
      'Wahl',
    );
    await userEvent.type(
      screen.getByLabelText('Abwägung (Deutsch)', { selector: '#decision-0-trade-off-de' }),
      'Preis',
    );
    await userEvent.click(screen.getByRole('button', { name: 'Bild hinzufügen' }));
    expect(
      within(screen.getByLabelText('Bild', { selector: '#image-0-document' })).getAllByRole('option'),
    ).toHaveLength(1);
    await userEvent.type(
      screen.getByLabelText('Bildunterschrift (Deutsch)', { selector: '#image-0-caption-de' }),
      'Start',
    );
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(mocks.projects.create).toHaveBeenCalledWith(
      expect.objectContaining({
        slug: 'neues-projekt',
        status: 'InProduction',
        stackTags: ['C#', 'PostgreSQL'],
        isVisible: true,
        isFeatured: false,
        de: expect.objectContaining({ title: 'Neu', teaser: 'Kurztext' }),
        decisions: [{ de: { title: 'Wahl', tradeOff: 'Preis' }, en: undefined }],
        images: [{ documentId: 21, captionDe: 'Start', captionEn: '' }],
      }),
    );
  });

  it('shows a conflict such as a taken slug', async () => {
    const conflict = {
      status: 409,
      title: 'Conflict.',
      detail: "The slug 'beispielprojekt' is already used.",
    };
    await renderEditor(clients({ update: vi.fn(() => throwError(() => conflict)) }));

    await userEvent.click(screen.getByRole('button', { name: /Beispielprojekt/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already used');
  });

  it('removes a decision before saving', async () => {
    const mocks = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispielprojekt/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Entfernen: Entscheidung' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(mocks.projects.update).toHaveBeenCalledWith(4, expect.objectContaining({ decisions: [] }));
  });
});
