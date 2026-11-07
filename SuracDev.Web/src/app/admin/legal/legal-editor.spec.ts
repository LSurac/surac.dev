import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { of, throwError } from 'rxjs';
import { AdminLegalPage, AdminLegalPagesClient } from '../../api/generated/admin.client';
import { LegalEditor } from './legal-editor';

const imprint: AdminLegalPage = {
  key: 'Imprint',
  de: { title: 'Impressum', bodyMarkdown: '## Angaben', updatedAt: '2026-03-05T12:00:00Z' },
  en: { title: 'Legal notice', bodyMarkdown: '## Details', updatedAt: '2026-03-06T12:00:00Z' },
};

const privacy: AdminLegalPage = {
  key: 'Privacy',
  de: { title: 'Datenschutzerklärung', bodyMarkdown: '## Daten', updatedAt: '2026-01-15T12:00:00Z' },
  en: undefined,
};

function client(update = vi.fn(() => of(undefined))) {
  return {
    list: vi.fn(() => of([structuredClone(privacy), structuredClone(imprint)])),
    update,
  };
}

async function renderEditor(pages = client()) {
  await render(LegalEditor, { providers: [{ provide: AdminLegalPagesClient, useValue: pages }] });
  await screen.findByRole('list', { name: 'Seiten' });
  return pages;
}

describe('admin legal editor', () => {
  it('offers the two fixed pages without a way to create or delete', async () => {
    await renderEditor();

    const list = screen.getByRole('list', { name: 'Seiten' });
    const pages = within(list).getAllByRole('button');

    expect(pages.map((page) => page.textContent?.trim())).toEqual(['Impressum', 'Datenschutz']);
    expect(screen.queryByRole('button', { name: 'Löschen' })).not.toBeInTheDocument();
  });

  it('loads the imprint first with both locales and their stamps', async () => {
    await renderEditor();

    expect(screen.getByRole('button', { name: 'Impressum' })).toHaveAttribute('aria-current', 'true');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    const english = screen.getByRole('group', { name: 'English' });
    expect(within(german).getByLabelText('Titel')).toHaveValue('Impressum');
    expect(within(german).getByLabelText(/Text \(Markdown\)/)).toHaveValue('## Angaben');
    expect(within(german).getByText('Stand: 05.03.2026')).toBeInTheDocument();
    expect(within(english).getByLabelText('Titel')).toHaveValue('Legal notice');
    expect(within(english).getByText('Stand: 06.03.2026')).toBeInTheDocument();
  });

  it('shows the other page after switching and marks the missing English text', async () => {
    await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Datenschutz' }));

    expect(screen.getByRole('button', { name: 'Datenschutz' })).toHaveAttribute('aria-current', 'true');
    expect(screen.getByRole('button', { name: 'Impressum' })).not.toHaveAttribute('aria-current');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText('Titel')).toHaveValue('Datenschutzerklärung');
    const english = screen.getByRole('group', { name: 'English' });
    expect(within(english).getByLabelText(/^Titel/)).toHaveValue('');
    expect(within(english).getByText('Titel').parentElement).toHaveTextContent('nicht übersetzt');
    expect(within(english).queryByText(/Stand:/)).not.toBeInTheDocument();
  });

  it('saves the shown page under its key with both locales', async () => {
    const pages = await renderEditor();

    const german = screen.getByRole('group', { name: 'Deutsch' });
    await userEvent.clear(within(german).getByLabelText('Titel'));
    await userEvent.type(within(german).getByLabelText('Titel'), 'Neu');
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(pages.update).toHaveBeenCalledWith('Imprint', {
      de: { title: 'Neu', bodyMarkdown: '## Angaben' },
      en: { title: 'Legal notice', bodyMarkdown: '## Details' },
    });
    expect(pages.list).toHaveBeenCalledTimes(2);
    expect(await screen.findByRole('status')).toHaveTextContent('Gespeichert.');
    expect(screen.getByRole('button', { name: 'Impressum' })).toHaveAttribute('aria-current', 'true');
  });

  it('saves the other page under its own key', async () => {
    const pages = await renderEditor();

    await userEvent.click(screen.getByRole('button', { name: 'Datenschutz' }));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(pages.update).toHaveBeenCalledWith('Privacy', {
      de: { title: 'Datenschutzerklärung', bodyMarkdown: '## Daten' },
      en: undefined,
    });
  });

  it('removes the English texts by emptying both English fields', async () => {
    const pages = await renderEditor();

    const english = screen.getByRole('group', { name: 'English' });
    await userEvent.clear(within(english).getByLabelText(/^Titel/));
    await userEvent.clear(within(english).getByLabelText(/Text \(Markdown\)/));
    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(pages.update).toHaveBeenCalledWith('Imprint', expect.objectContaining({ en: undefined }));
  });

  it('shows the errors of a rejected save', async () => {
    const problem = { status: 400, errors: { 'de.bodyMarkdown': ['Required.'] } };
    await renderEditor(client(vi.fn(() => throwError(() => problem))));

    await userEvent.click(screen.getByRole('button', { name: 'Speichern' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Required.');
    const german = screen.getByRole('group', { name: 'Deutsch' });
    expect(within(german).getByLabelText(/Text \(Markdown\)/).parentElement).toHaveTextContent('Required.');
  });
});
