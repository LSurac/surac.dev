import { render, screen } from '@testing-library/angular';
import { UiField } from './field';

describe('ui-field', () => {
  it('labels the projected control and shows its hint', async () => {
    await render(
      `<ui-field label="Überschrift" controlId="headline" hint="Ein Satz."><input id="headline" /></ui-field>`,
      { imports: [UiField] },
    );

    expect(screen.getByLabelText('Überschrift')).toHaveAttribute('id', 'headline');
    expect(screen.getByText('Ein Satz.')).toBeInTheDocument();
  });

  it('shows every error and a badge next to the label', async () => {
    await render(
      `<ui-field label="Headline" controlId="en-headline" badge="nicht übersetzt" [errors]="errors">
        <input id="en-headline" />
      </ui-field>`,
      { imports: [UiField], componentProperties: { errors: ['Required.', 'Too long.'] } },
    );

    expect(screen.getByText('nicht übersetzt')).toBeInTheDocument();
    expect(screen.getByText('Required.')).toBeInTheDocument();
    expect(screen.getByText('Too long.')).toBeInTheDocument();
  });

  it('shows neither hint nor errors when there are none', async () => {
    const { container } = await render(
      `<ui-field label="Ort" controlId="location"><input id="location" /></ui-field>`,
      { imports: [UiField] },
    );

    expect(container.querySelectorAll('p')).toHaveLength(0);
  });
});
