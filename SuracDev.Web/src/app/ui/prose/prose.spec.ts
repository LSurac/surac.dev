import { render, screen } from '@testing-library/angular';
import { UiProse } from './prose';

describe('ui-prose', () => {
  it('renders the html the API rendered from Markdown', async () => {
    await render(UiProse, {
      inputs: { html: '<h2>Abschnitt</h2>\n<p><strong>Text</strong> mit <a href="/impressum">Link</a></p>' },
    });

    expect(screen.getByRole('heading', { level: 2, name: 'Abschnitt' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Link' })).toHaveAttribute('href', '/impressum');
  });

  it('still strips anything a sanitizer would remove', async () => {
    const { container } = await render(UiProse, {
      inputs: { html: '<p onclick="alert(1)">Text</p><script>alert(2)</script>' },
    });

    expect(container.querySelector('script')).toBeNull();
    expect(screen.getByText('Text')).not.toHaveAttribute('onclick');
  });
});
