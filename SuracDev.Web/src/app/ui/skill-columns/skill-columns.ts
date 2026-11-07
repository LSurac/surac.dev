import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One skill: a technology, since when, and an optional qualifier. Never a level. */
export interface SkillItem {
  readonly name: string;
  readonly sinceYear?: number | null;
  readonly qualifier?: string | null;
}

/** One column of `ui-skill-columns`. */
export interface SkillColumn {
  readonly id: number;
  readonly name: string;
  readonly skills: readonly SkillItem[];
}

/** Skills in up to three columns with hairline rows and the "since" year right-aligned (docs/design.md). */
@Component({
  selector: 'ui-skill-columns',
  templateUrl: './skill-columns.html',
  styleUrl: './skill-columns.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiSkillColumns {
  readonly groups = input.required<readonly SkillColumn[]>();
  readonly sinceLabel = input.required<string>();
}
