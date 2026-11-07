import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * German and English content side by side, as the admin edits every translatable entity. Content
 * marked with the `de` attribute goes left, `en` right; on narrow screens they stack.
 */
@Component({
  selector: 'ui-locale-pair',
  templateUrl: './locale-pair.html',
  styleUrl: './locale-pair.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiLocalePair {
  readonly germanLabel = input.required<string>();
  readonly englishLabel = input.required<string>();
}
