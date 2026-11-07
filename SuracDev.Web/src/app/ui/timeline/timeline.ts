import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One entry of a `ui-timeline`: a career station or an education entry. */
export interface TimelineEntry {
  readonly id: number;
  readonly period: string;
  readonly title: string;
  readonly organisation: string;
  readonly location?: string | null;
  readonly summary?: string | null;
  readonly bullets: readonly string[];
  readonly detail?: string | null;
}

/** The career timeline: a date column and the entry, dotted rules between entries (docs/design.md). */
@Component({
  selector: 'ui-timeline',
  templateUrl: './timeline.html',
  styleUrl: './timeline.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiTimeline {
  readonly label = input.required<string>();
  readonly entries = input.required<readonly TimelineEntry[]>();
}
