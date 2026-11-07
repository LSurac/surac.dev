import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Long-form text the API rendered from the restricted Markdown subset (case studies, legal pages).
 * The API escapes everything; Angular's sanitizer still runs on the binding as a second line.
 */
@Component({
  selector: 'ui-prose',
  template: '<div class="prose" [innerHTML]="html()"></div>',
  styleUrl: './prose.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiProse {
  readonly html = input.required<string>();
}
