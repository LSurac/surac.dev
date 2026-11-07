import { render, screen } from '@testing-library/angular';
import { HomePage } from './home-page';

describe('app-home-page', () => {
  it('opens with the title line as kicker and one level-one heading, in German by default', async () => {
    await render(HomePage);

    expect(screen.getByText('Fullstack-Entwickler · .NET & Angular')).toBeInTheDocument();
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
    expect(screen.getByRole('heading', { level: 2, name: 'Projekte' })).toBeInTheDocument();
  });

  it('renders the English texts for the English locale', async () => {
    await render(HomePage, { inputs: { locale: 'en' } });

    expect(screen.getByText('Full-stack developer · .NET & Angular')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Projects' })).toBeInTheDocument();
  });
});
