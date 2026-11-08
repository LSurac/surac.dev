import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of } from 'rxjs';
import {
  AdminDocument,
  AdminDocumentsClient,
  AdminInterest,
  AdminInterestsClient,
} from '../../api/generated/admin.client';
import { InterestEditor } from './interest-editor';

const chess: AdminInterest = {
  id: 1,
  kind: 'Chess',
  sortOrder: 0,
  isVisible: true,
  imageDocumentId: 7,
  de: { title: 'Schach', body: 'Spiele im **Verein**.' },
  en: { title: 'Chess', body: undefined },
};

const hidden: AdminInterest = {
  ...chess,
  id: 2,
  kind: 'Music',
  isVisible: false,
  imageDocumentId: undefined,
  de: { title: 'Musik' },
  en: undefined,
};

const board: AdminDocument = {
  id: 7,
  fileName: 'brett.webp',
  contentType: 'image/webp',
  visibility: 'Public',
  labelDe: 'Schachbrett',
};

const certificate: AdminDocument = {
  ...board,
  id: 8,
  fileName: 'zeugnis.pdf',
  contentType: 'application/pdf',
  visibility: 'LinkOnly',
  labelDe: 'Zeugnis',
};

async function renderEditor() {
  const interests = {
    list: vi.fn(() => of([structuredClone(chess), structuredClone(hidden)])),
    create: vi.fn((form) => of({ ...form, id: 3 })),
    update: vi.fn(() => of(undefined)),
    delete: vi.fn(() => of(undefined)),
  };
  await render(InterestEditor, {
    providers: [
      { provide: AdminInterestsClient, useValue: interests },
      { provide: AdminDocumentsClient, useValue: { list: vi.fn(() => of([board, certificate])) } },
    ],
  });
  await screen.findByRole('list', { name: 'Abschnitte' });
  return interests;
}

describe('admin interest editor', () => {
  it('lists the sections with their topic and marks hidden ones', async () => {
    await renderEditor();

    expect(screen.getByRole('button', { name: /Schach.*Schach/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Musik.*Musik/ })).toBeInTheDocument();
  });

  it('fills the form with the chosen section, its image and both locales', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /^Schach/ }));

    expect(screen.getByLabelText('Thema')).toHaveDisplayValue('Schach');
    expect(screen.getByLabelText('Bild')).toHaveDisplayValue('Schachbrett (brett.webp)');
    expect(
      within(screen.getByRole('group', { name: 'Deutsch' })).getByLabelText('Text (Markdown)'),
    ).toHaveValue('Spiele im **Verein**.');
    expect(within(screen.getByRole('group', { name: 'English' })).getByLabelText('Titel')).toHaveValue(
      'Chess',
    );
  });

  it('offers only public images', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neuer Abschnitt' }));

    const options = within(screen.getByLabelText('Bild')).getAllByRole('option');
    expect(options.map((option) => option.textContent?.trim())).toEqual([
      'Kein Bild',
      'Schachbrett (brett.webp)',
    ]);
  });

  it('creates a section with topic, image and texts', async () => {
    const interests = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neuer Abschnitt' }));
    await userEvent.selectOptions(screen.getByLabelText('Thema'), 'Homelab');
    await userEvent.selectOptions(screen.getByLabelText('Bild'), 'Schachbrett (brett.webp)');
    await userEvent.type(
      within(screen.getByRole('group', { name: 'Deutsch' })).getByLabelText('Titel'),
      'Homelab',
    );
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(interests.create).toHaveBeenCalledWith({
      kind: 'Homelab',
      sortOrder: 0,
      isVisible: true,
      imageDocumentId: 7,
      de: { title: 'Homelab', body: '' },
      en: { title: '', body: '' },
    });
  });

  it('deletes a section only after confirmation', async () => {
    const interests = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /^Schach/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));

    expect(interests.delete).toHaveBeenCalledWith(1);
  });
});
