import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary';

/**
 * The one button of the design system. Renders a link when `href` is set (downloads, mailto),
 * otherwise a real button. Presentational: inputs and outputs only.
 */
@Component({
  selector: 'ui-button',
  templateUrl: './button.html',
  styleUrl: './button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiButton {
  readonly label = input.required<string>();
  readonly variant = input<ButtonVariant>('secondary');
  readonly href = input<string | null>(null);
  readonly download = input<string | null>(null);
  readonly type = input<'button' | 'submit'>('button');
  readonly disabled = input(false);

  readonly pressed = output<void>();
}
