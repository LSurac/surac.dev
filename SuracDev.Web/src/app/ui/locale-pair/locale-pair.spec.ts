import { render, screen, within } from '@testing-library/angular';
import { UiLocalePair } from './locale-pair';

describe('ui-locale-pair', () => {
  it('puts the German and the English content side by side under their headings', async () => {
    await render(
      `<ui-locale-pair germanLabel="Deutsch" englishLabel="English">
        <p de>Deutscher Text</p>
        <p en>English text</p>
      </ui-locale-pair>`,
      { imports: [UiLocalePair] },
    );

    const german = screen.getByRole('group', { name: 'Deutsch' });
    const english = screen.getByRole('group', { name: 'English' });

    expect(within(german).getByText('Deutscher Text')).toBeInTheDocument();
    expect(within(english).getByText('English text')).toBeInTheDocument();
    expect(english).toHaveAttribute('lang', 'en');
  });
});
