import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The numbered small-caps label that opens every section ("01 · Profil"). Sections are an `h2` for
 * the document outline; the serif is reserved for the hero and project titles (docs/design.md).
 */
@Component({
  selector: 'ui-section-heading',
  templateUrl: './section-heading.html',
  styleUrl: './section-heading.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiSectionHeading {
  readonly number = input.required<string>();
  readonly title = input.required<string>();
  readonly anchor = input<string | null>(null);
}
