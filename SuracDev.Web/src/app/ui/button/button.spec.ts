import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { UiButton } from './button';

describe('ui-button', () => {
  it('renders a real button that emits when pressed', async () => {
    const pressed = vi.fn();
    await render(UiButton, { inputs: { label: 'E-Mail schreiben' }, on: { pressed } });

    await userEvent.click(screen.getByRole('button', { name: 'E-Mail schreiben' }));

    expect(pressed).toHaveBeenCalledTimes(1);
  });

  it('renders a link with a download name when an href is given', async () => {
    await render(UiButton, {
      inputs: {
        label: 'Lebenslauf (PDF)',
        href: '/api/public/cv.pdf',
        download: 'Lebenslauf_Leonard_Surac.pdf',
      },
    });

    const link = screen.getByRole('link', { name: 'Lebenslauf (PDF)' });

    expect(link).toHaveAttribute('href', '/api/public/cv.pdf');
    expect(link).toHaveAttribute('download', 'Lebenslauf_Leonard_Surac.pdf');
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('marks the primary variant and respects disabled', async () => {
    await render(UiButton, {
      inputs: { label: 'Speichern', variant: 'primary', disabled: true, type: 'submit' },
    });

    const button = screen.getByRole('button', { name: 'Speichern' });

    expect(button).toHaveClass('button--primary');
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('type', 'submit');
  });
});
