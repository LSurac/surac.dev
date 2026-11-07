import { render, screen } from '@testing-library/angular';
import { UiSectionHeading } from './section-heading';

describe('ui-section-heading', () => {
  it('is a level-two heading whose accessible name is the title without the number', async () => {
    await render(UiSectionHeading, { inputs: { number: '02', title: 'Projekte', anchor: 'projekte' } });

    const heading = screen.getByRole('heading', { level: 2, name: 'Projekte' });

    expect(heading).toHaveAttribute('id', 'projekte');
    expect(heading).toHaveTextContent('02');
  });

  it('has no id when no anchor is given', async () => {
    await render(UiSectionHeading, { inputs: { number: '01', title: 'Profil' } });

    expect(screen.getByRole('heading', { level: 2 })).not.toHaveAttribute('id');
  });
});
