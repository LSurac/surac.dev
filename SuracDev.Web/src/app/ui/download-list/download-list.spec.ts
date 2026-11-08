import { render, screen, within } from '@testing-library/angular';
import { UiDownloadList } from './download-list';

describe('ui-download-list', () => {
  it('lists each download as a link that saves the file under the name the server sends', async () => {
    await render(UiDownloadList, {
      inputs: {
        label: 'Unterlagen',
        items: [
          { label: 'Anschreiben', href: '/api/links/abc/documents/3' },
          { label: 'Arbeitszeugnis', href: '/api/links/abc/documents/4' },
        ],
      },
    });

    const list = screen.getByRole('list', { name: 'Unterlagen' });
    const links = within(list).getAllByRole('link');

    expect(links.map((link) => link.textContent?.trim())).toEqual(['Anschreiben', 'Arbeitszeugnis']);
    expect(links[0]).toHaveAttribute('href', '/api/links/abc/documents/3');
    expect(links[0]).toHaveAttribute('download', '');
    expect(screen.getByText('Unterlagen')).toBeInTheDocument();
  });
});
