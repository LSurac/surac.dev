import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Fact } from './fact';

/** The row of up to four key facts under the hero (docs/design.md, "Fact bar"). */
@Component({
  selector: 'ui-fact-bar',
  templateUrl: './fact-bar.html',
  styleUrl: './fact-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiFactBar {
  readonly facts = input.required<readonly Fact[]>();
}
