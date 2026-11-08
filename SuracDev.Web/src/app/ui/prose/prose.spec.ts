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

  it('keeps a code excerpt together with the file it comes from', async () => {
    const { container } = await render(UiProse, {
      inputs: {
        html:
          '<figure class="excerpt"><figcaption><code>Domains/X/Y.cs</code></figcaption>' +
          '<pre><code class="language-csharp">public void Revoke();</code></pre></figure>',
      },
    });

    const excerpt = screen.getByRole('figure');
    expect(excerpt).toHaveClass('excerpt');
    expect(excerpt.querySelector('figcaption')).toHaveTextContent('Domains/X/Y.cs');
    expect(container.querySelector('pre code.language-csharp')).toHaveTextContent('public void Revoke();');
  });

  it('still strips anything a sanitizer would remove', async () => {
    const { container } = await render(UiProse, {
      inputs: { html: '<p onclick="alert(1)">Text</p><script>alert(2)</script>' },
    });

    expect(container.querySelector('script')).toBeNull();
    expect(screen.getByText('Text')).not.toHaveAttribute('onclick');
  });
});
