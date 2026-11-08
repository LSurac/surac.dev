import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import {
  AdminApplication,
  AdminApplicationLinksClient,
  AdminApplicationsClient,
  AdminDocumentsClient,
} from '../../api/generated/admin.client';
import { ApplicationEditor } from './application-editor';

const sent: AdminApplication = {
  id: 1,
  company: 'Beispiel AG',
  position: 'Fullstack-Entwickler',
  jobAdUrl: 'https://example.org/jobs/1',
  appliedOn: '2026-11-01',
  status: 'Sent',
  notes: 'Kontakt über Recruiterin',
  createdAt: '2026-11-01T09:00:00Z',
  updatedAt: '2026-11-01T09:00:00Z',
};

function client(overrides: Partial<Record<'create' | 'update' | 'delete', ReturnType<typeof vi.fn>>> = {}) {
  return {
    list: vi.fn(() => of([structuredClone(sent)])),
    create: overrides.create ?? vi.fn((form) => of({ ...form, id: 2 })),
    update: overrides.update ?? vi.fn(() => of(undefined)),
    delete: overrides.delete ?? vi.fn(() => of(undefined)),
  };
}

async function renderEditor(applications = client()) {
  await render(ApplicationEditor, {
    providers: [
      { provide: AdminApplicationsClient, useValue: applications },
      { provide: AdminApplicationLinksClient, useValue: { list: vi.fn(() => of([])) } },
      { provide: AdminDocumentsClient, useValue: { list: vi.fn(() => of([])) } },
    ],
  });
  await screen.findByRole('list', { name: 'Bewerbungen' });
  return applications;
}

describe('admin application editor', () => {
  it('lists the applications with position and status', async () => {
    await renderEditor();

    expect(
      screen.getByRole('button', { name: /Beispiel AG.*Fullstack-Entwickler · Versendet/ }),
    ).toBeInTheDocument();
  });

  it('fills the form with the chosen application and shows its links', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel AG/ }));

    expect(screen.getByLabelText('Firma')).toHaveValue('Beispiel AG');
    expect(screen.getByLabelText('Beworben am')).toHaveValue('2026-11-01');
    expect(screen.getByLabelText('Status')).toHaveValue('Sent');
    expect(screen.getByLabelText('Notizen (Markdown)')).toHaveValue('Kontakt über Recruiterin');
    expect(await screen.findByRole('heading', { level: 2, name: 'Bewerbungs-Links' })).toBeInTheDocument();
  });

  it('creates a new application as a draft by default', async () => {
    const applications = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Neue Bewerbung' }));
    await userEvent.type(screen.getByLabelText('Firma'), 'Neue GmbH');
    await userEvent.type(screen.getByLabelText('Stelle'), 'Backend-Entwickler');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(applications.create).toHaveBeenCalledWith({
      company: 'Neue GmbH',
      position: 'Backend-Entwickler',
      jobAdUrl: undefined,
      appliedOn: undefined,
      status: 'Draft',
      notes: undefined,
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
  });

  it('shows the field errors of a rejected save', async () => {
    await renderEditor(
      client({
        create: vi.fn(() =>
          throwError(() => ({ status: 400, title: 'Invalid', errors: { company: ['Required.'] } })),
        ),
      }),
    );

    await userEvent.click(screen.getByRole('button', { name: 'Neue Bewerbung' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Required.');
  });

  it('deletes an application only after confirmation', async () => {
    const applications = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: /Beispiel AG/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Löschen' }));
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig löschen' }));

    expect(applications.delete).toHaveBeenCalledWith(1);
  });
});
