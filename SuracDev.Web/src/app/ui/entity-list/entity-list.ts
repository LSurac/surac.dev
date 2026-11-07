import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** One entry of a `ui-entity-list`. `muted` marks entries the open site does not show. */
export interface EntityListItem {
  readonly id: number;
  readonly title: string;
  readonly meta?: string;
  readonly muted?: boolean;
}

/** The selectable list beside an admin editor, with a button to start a new entry. */
@Component({
  selector: 'ui-entity-list',
  templateUrl: './entity-list.html',
  styleUrl: './entity-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiEntityList {
  readonly items = input.required<readonly EntityListItem[]>();
  readonly selectedId = input<number | null>(null);
  readonly label = input.required<string>();
  readonly createLabel = input.required<string>();

  readonly selected = output<number>();
  readonly created = output<void>();
}
