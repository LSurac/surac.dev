import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminDocument, AdminDocumentsClient } from '../../api/generated/admin.client';
import { DocumentManager } from './document-manager';

const photo: AdminDocument = {
  id: 7,
  fileName: 'portrait.jpg',
  contentType: 'image/jpeg',
  sizeBytes: 204_800,
  kind: 'Photo',
  visibility: 'Public',
  uploadedAt: '2026-11-07T12:00:00Z',
  labelDe: 'Porträt',
  labelEn: undefined,
};

const certificate: AdminDocument = {
  id: 8,
  fileName: 'zeugnis.pdf',
  contentType: 'application/pdf',
  sizeBytes: 1_048_576,
  kind: 'Certificate',
  visibility: 'LinkOnly',
  uploadedAt: '2026-11-06T12:00:00Z',
  labelDe: undefined,
  labelEn: undefined,
};

function client(overrides: Partial<Record<'update' | 'delete', ReturnType<typeof vi.fn>>> = {}) {
  return {
    list: vi.fn(() => of([structuredClone(photo), structuredClone(certificate)])),
    upload: vi.fn(() => of({ ...certificate, id: 9, fileName: 'neu.pdf' })),
    update: overrides.update ?? vi.fn(() => of(undefined)),
    delete: overrides.delete ?? vi.fn(() => of(undefined)),
  };
}

async function renderManager(documents = client()) {
  await render(DocumentManager, { providers: [{ provide: AdminDocumentsClient, useValue: documents }] });
  await screen.findByRole('list', { name: 'Dokumente' });
  return documents;
}

describe('admin document manager', () => {
  it('lists documents with kind and visibility and previews public images', async () => {
    await renderManager();

    expect(screen.getByRole('button', { name: /Porträt.*Foto · Öffentlich/ })).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /zeugnis\.pdf.*Zeugnis · Nur über Bewerbungs-Link/ }),
    ).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /Porträt/ }));

    expect(screen.getByRole('img', { name: 'Porträt' })).toHaveAttribute('src', '/api/public/documents/7');
    expect(screen.getByRole('link', { name: 'Herunterladen' })).toHaveAttribute(
      'href',
      '/api/admin/documents/7/content',
    );
  });

  it('uploads a file with its kind, visibility and labels', async () => {
    const documents = await renderManager();
    const file = new File(['%PDF-1.7'], 'neu.pdf', { type: 'application/pdf' });

    await userEvent.click(screen.getByRole('button', { name: 'Dokument hochladen' }));
    await userEvent.upload(screen.getByLabelText('Datei'), file);
    await userEvent.selectOptions(screen.getByLabelText('Art'), 'Zeugnis');
    await userEvent.selectOptions(screen.getByLabelText('Sichtbarkeit'), 'Nur über Bewerbungs-Link');
    await userEvent.type(screen.getByLabelText('Bezeichnung (Deutsch)'), 'Zeugnis 2024');
    await userEvent.click(screen.getByRole('button', { name: 'Hochladen' }));

    expect(documents.upload).toHaveBeenCalledWith(
      { data: file, fileName: 'neu.pdf' },
      'Certificate',
      'LinkOnly',
      'Zeugnis 2024',
      '',
    );
    expect(documents.list).toHaveBeenCalledTimes(2);
  });

  it('asks for a file before uploading', async () => {
    const documents = await renderManager();

    await userEvent.click(screen.getByRole('button', { name: 'Dokument hochladen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Hochladen' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Bitte eine Datei wählen.');
    expect(documents.upload).not.toHaveBeenCalled();
  });

  it('changes visibility and labels of a document', async () => {
    const documents = await renderManager();

    await userEvent.click(screen.getByRole('button', { name: /zeugnis\.pdf/ }));
    await userEvent.type(screen.getByLabelText('Bezeichnung (English)'), 'Reference');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(documents.update).toHaveBeenCalledWith(8, {
      visibility: 'LinkOnly',
      labelDe: '',
      labelEn: 'Reference',
    });
  });

  it('explains why a document in use cannot be deleted', async () => {
    const conflict = { status: 409, title: 'Conflict.', detail: 'The document is still in use.' };
    await renderManager(client({ delete: vi.fn(() => throwError(() => conflict)) }));

    await userEvent.click(screen.getByRole('button', { name: /Porträt/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('still in use');
  });
});
