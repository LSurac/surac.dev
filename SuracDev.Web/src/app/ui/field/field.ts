import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A form field frame: label, the projected control, an optional hint, a badge next to the label
 * ("nicht übersetzt") and the field's errors. The control itself is projected so reactive forms
 * bind it directly; `controlId` must match the control's id.
 */
@Component({
  selector: 'ui-field',
  templateUrl: './field.html',
  styleUrl: './field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiField {
  readonly label = input.required<string>();
  readonly controlId = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly badge = input<string | null>(null);
  readonly errors = input<readonly string[]>([]);
}
