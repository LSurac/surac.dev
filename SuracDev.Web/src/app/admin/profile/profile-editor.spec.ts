import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminDocumentsClient, AdminProfile, AdminProfileClient } from '../../api/generated/admin.client';
import { ProfileEditor } from './profile-editor';

const profile: AdminProfile = {
  fullName: 'Max Mustermann',
  email: 'max@example.org',
  phone: undefined,
  location: 'Musterstadt',
  addressLines: ['Musterstraße 1', '00000 Musterstadt'],
  gitHubUrl: 'https://github.com/example',
  linkedInUrl: undefined,
  photoDocumentId: undefined,
  de: {
    titleLine: 'Fullstack-Entwickler',
    headline: 'Überschrift.',
    intro: 'Kurzprofil.',
    availabilityLine: undefined,
  },
  en: {
    titleLine: 'Full-stack developer',
    headline: undefined,
    intro: undefined,
    availabilityLine: undefined,
  },
  facts: [{ value: '5', labelDe: 'Jahre', labelEn: 'Years' }],
};

async function renderEditor(put = vi.fn(() => of(undefined))) {
  const profileClient = { get: vi.fn(() => of(structuredClone(profile))), put };
  const documents = {
    list: vi.fn(() =>
      of([
        {
          id: 7,
          fileName: 'foto.jpg',
          contentType: 'image/jpeg',
          kind: 'Photo',
          visibility: 'Public',
          labelDe: 'Porträt',
        },
        {
          id: 8,
          fileName: 'zeugnis.pdf',
          contentType: 'application/pdf',
          kind: 'Certificate',
          visibility: 'LinkOnly',
        },
      ]),
    ),
  };
  await render(ProfileEditor, {
    providers: [
      { provide: AdminProfileClient, useValue: profileClient },
      { provide: AdminDocumentsClient, useValue: documents },
    ],
  });
  await screen.findByDisplayValue('Max Mustermann');
  return { put };
}

describe('admin profile editor', () => {
  it('shows German and English texts side by side', async () => {
    await renderEditor();

    const german = screen.getByRole('group', { name: 'Deutsch' });
    const english = screen.getByRole('group', { name: 'English' });

    expect(within(german).getByLabelText(/Titelzeile/)).toHaveValue('Fullstack-Entwickler');
    expect(within(english).getByLabelText(/Titelzeile/)).toHaveValue('Full-stack developer');
    expect(screen.getByLabelText('Anschrift')).toHaveValue('Musterstraße 1\n00000 Musterstadt');
  });

  it('marks English fields that fall back to German', async () => {
    await renderEditor();

    const english = screen.getByRole('group', { name: 'English' });

    expect(within(english).getByText('Überschrift').parentElement).toHaveTextContent('nicht übersetzt');
    expect(within(english).getByText('Titelzeile').parentElement).not.toHaveTextContent('nicht übersetzt');
  });

  it('offers only public images as photo', async () => {
    await renderEditor();

    const options = within(screen.getByLabelText('Foto')).getAllByRole('option');

    expect(options.map((option) => option.textContent?.trim())).toEqual(['Kein Foto', 'Porträt (foto.jpg)']);
  });

  it('saves the edited profile with both locales and the fact bar', async () => {
    const { put } = await renderEditor();

    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.clear(within(german).getByLabelText(/Überschrift/));
    await userEvent.type(within(german).getByLabelText(/Überschrift/), 'Neu.');
    await userEvent.click(screen.getByRole('button', { name: 'Kennzahl hinzufügen' }));
    await userEvent.type(screen.getByLabelText('Wert', { selector: '#fact-1-value' }), 'C1');
    await userEvent.type(
      screen.getByLabelText('Bezeichnung (Deutsch)', { selector: '#fact-1-label-de' }),
      'Englisch',
    );
    await userEvent.selectOptions(screen.getByLabelText('Foto'), 'Porträt (foto.jpg)');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(put).toHaveBeenCalledWith(
      expect.objectContaining({
        fullName: 'Max Mustermann',
        addressLines: ['Musterstraße 1', '00000 Musterstadt'],
        photoDocumentId: 7,
        de: expect.objectContaining({ headline: 'Neu.' }),
        en: expect.objectContaining({ titleLine: 'Full-stack developer' }),
        facts: [
          { value: '5', labelDe: 'Jahre', labelEn: 'Years' },
          { value: 'C1', labelDe: 'Englisch', labelEn: '' },
        ],
      }),
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
  });

  it('removes a fact', async () => {
    const { put } = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Entfernen: 5' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(put).toHaveBeenCalledWith(expect.objectContaining({ facts: [] }));
  });

  it('shows the errors of a rejected save next to the fields', async () => {
    const problem = { status: 400, title: 'Validation failed.', errors: { 'de.headline': ['Required.'] } };
    await renderEditor(vi.fn(() => throwError(() => problem)));

    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Required.');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText(/Überschrift/).parentElement).toHaveTextContent('Required.');
  });
});
