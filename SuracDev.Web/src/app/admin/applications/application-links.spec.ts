import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of } from 'rxjs';
import {
  AdminApplicationLink,
  AdminApplicationLinksClient,
  AdminDocument,
  AdminDocumentsClient,
} from '../../api/generated/admin.client';
import { ApplicationLinks } from './application-links';

const active: AdminApplicationLink = {
  id: 10,
  applicationId: 1,
  profile: 'Recruiter',
  greetingDe: 'Für das Team Backend',
  greetingEn: undefined,
  expiresAt: '2027-02-06T12:00:00Z',
  isRevoked: false,
  isActive: true,
  unlockFullPdf: true,
  documentIds: [5],
  createdAt: '2026-11-08T12:00:00Z',
};

const revoked: AdminApplicationLink = {
  ...active,
  id: 11,
  profile: 'TechLead',
  isRevoked: true,
  isActive: false,
  unlockFullPdf: false,
  documentIds: [],
};

const certificate: AdminDocument = {
  id: 5,
  fileName: 'zeugnis.pdf',
  contentType: 'application/pdf',
  sizeBytes: 1000,
  kind: 'Certificate',
  visibility: 'LinkOnly',
  uploadedAt: '2026-11-01T09:00:00Z',
  labelDe: 'Zeugnis',
  labelEn: undefined,
};

const photo: AdminDocument = { ...certificate, id: 6, kind: 'Photo', visibility: 'Public', labelDe: 'Foto' };

async function renderLinks() {
  const links = {
    list: vi.fn(() => of([structuredClone(active), structuredClone(revoked)])),
    create: vi.fn(() => of({ token: 'tok_EN-123', link: { ...active, id: 12 } })),
    update: vi.fn(() => of(undefined)),
    revoke: vi.fn(() => of(undefined)),
  };
  await render(ApplicationLinks, {
    inputs: { applicationId: 1 },
    providers: [
      { provide: AdminApplicationLinksClient, useValue: links },
      { provide: AdminDocumentsClient, useValue: { list: vi.fn(() => of([certificate, photo])) } },
    ],
  });
  await screen.findByRole('list', { name: 'Links dieser Bewerbung' });
  return links;
}

describe('admin application links', () => {
  it('lists the links with audience, state, expiry and what they unlock', async () => {
    await renderLinks();

    const items = within(screen.getByRole('list', { name: 'Links dieser Bewerbung' })).getAllByRole(
      'listitem',
    );

    expect(items[0]).toHaveTextContent(
      'Recruiting · aktiv · bis 06.02.2027 · vollständiges PDF · 1 Unterlagen',
    );
    expect(items[1]).toHaveTextContent('Tech Lead · widerrufen');
    expect(within(items[1]!).queryByRole('button', { name: 'Widerrufen' })).not.toBeInTheDocument();
  });

  it('creates a link with the chosen settings and shows its addresses once', async () => {
    const links = await renderLinks();

    await userEvent.click(screen.getByRole('button', { name: 'Neuer Link' }));
    await userEvent.selectOptions(screen.getByLabelText('Zielgruppe'), 'TechLead');
    await userEvent.type(screen.getByLabelText('Begrüßung (Deutsch)'), 'Hallo Team');
    await userEvent.click(screen.getByRole('checkbox', { name: 'Zeugnis' }));
    await userEvent.click(screen.getByRole('button', { name: 'Link erzeugen' }));

    expect(links.create).toHaveBeenCalledWith(1, {
      profile: 'TechLead',
      greetingDe: 'Hallo Team',
      greetingEn: undefined,
      expiresAt: undefined,
      unlockFullPdf: false,
      documentIds: [5],
    });
    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('nur jetzt');
    expect((screen.getByLabelText('Adresse (Deutsch)') as HTMLInputElement).value).toMatch(
      /\/b\/tok_EN-123$/,
    );
    expect((screen.getByLabelText('Adresse (English)') as HTMLInputElement).value).toMatch(
      /\/en\/b\/tok_EN-123$/,
    );
  });

  it('offers only link-only documents for a link', async () => {
    await renderLinks();

    await userEvent.click(screen.getByRole('button', { name: 'Neuer Link' }));

    expect(screen.getByRole('checkbox', { name: 'Zeugnis' })).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'Foto' })).not.toBeInTheDocument();
  });

  it('edits a link with its current settings', async () => {
    const links = await renderLinks();

    const first = within(screen.getByRole('list', { name: 'Links dieser Bewerbung' })).getAllByRole(
      'listitem',
    )[0]!;
    await userEvent.click(within(first).getByRole('button', { name: 'Bearbeiten' }));

    expect(screen.getByLabelText('Zielgruppe')).toHaveValue('Recruiter');
    expect(screen.getByLabelText('Gültig bis')).toHaveValue('2027-02-06');
    expect(screen.getByRole('checkbox', { name: 'Zeugnis' })).toBeChecked();
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(links.update).toHaveBeenCalledWith(
      1,
      10,
      expect.objectContaining({ profile: 'Recruiter', unlockFullPdf: true, documentIds: [5] }),
    );
  });

  it('revokes a link only after confirmation', async () => {
    const links = await renderLinks();

    await userEvent.click(screen.getByRole('button', { name: 'Widerrufen' }));
    expect(links.revoke).not.toHaveBeenCalled();
    await userEvent.click(screen.getByRole('button', { name: 'Endgültig widerrufen' }));

    expect(links.revoke).toHaveBeenCalledWith(1, 10);
  });
});
