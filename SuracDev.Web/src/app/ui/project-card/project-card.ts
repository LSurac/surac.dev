import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** An external link of a project card (live product, source code). */
export interface ProjectCardLink {
  readonly label: string;
  readonly href: string;
}

/** A project tile: serif title, status line, teaser, stack line and two text links (docs/design.md). */
@Component({
  selector: 'ui-project-card',
  imports: [RouterLink],
  templateUrl: './project-card.html',
  styleUrl: './project-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiProjectCard {
  readonly title = input.required<string>();
  readonly teaser = input.required<string>();
  readonly status = input.required<string>();
  readonly period = input<string | null>(null);
  readonly stackTags = input<readonly string[]>([]);
  readonly caseStudyPath = input.required<string>();
  readonly caseStudyLabel = input.required<string>();
  readonly links = input<readonly ProjectCardLink[]>([]);

  protected readonly statusLine = computed(() =>
    [this.status(), this.period()].filter((part) => !!part).join(' · '),
  );
  protected readonly stackLine = computed(() => this.stackTags().join(' · '));
}
