import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { UiButton } from '../button/button';

/** A plain text link of the hero (GitHub, LinkedIn). */
export interface HeroLink {
  readonly label: string;
  readonly href: string;
}

/**
 * The download offered as primary action (the CV PDF). An empty `fileName` lets the browser take
 * the name the server sends in Content-Disposition.
 */
export interface HeroDownload {
  readonly label: string;
  readonly href: string;
  readonly fileName: string;
}

/**
 * The first screen, HR-first (docs/design.md): name, title line, headline, location, the CV and
 * e-mail actions, profile links and the photo. Nothing moves.
 */
@Component({
  selector: 'ui-hero',
  imports: [UiButton],
  templateUrl: './hero.html',
  styleUrl: './hero.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiHero {
  readonly name = input.required<string>();
  readonly titleLine = input.required<string>();
  readonly headline = input.required<string>();
  readonly location = input.required<string>();
  readonly email = input.required<string>();
  readonly emailLabel = input.required<string>();
  readonly links = input<readonly HeroLink[]>([]);
  readonly download = input<HeroDownload | null>(null);
  readonly photoUrl = input<string | null>(null);
  readonly photoAlt = input('');

  protected readonly mailto = computed(() => `mailto:${this.email()}`);
}
