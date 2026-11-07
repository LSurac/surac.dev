import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** The messages of a failed save, announced once as an alert above the form. */
@Component({
  selector: 'ui-error-summary',
  templateUrl: './error-summary.html',
  styleUrl: './error-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiErrorSummary {
  readonly title = input.required<string>();
  readonly messages = input.required<readonly string[]>();
}
