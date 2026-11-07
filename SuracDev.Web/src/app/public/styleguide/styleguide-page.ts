import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UiButton } from '../../ui/button/button';
import { Fact } from '../../ui/fact-bar/fact';
import { UiFactBar } from '../../ui/fact-bar/fact-bar';
import { UiSectionHeading } from '../../ui/section-heading/section-heading';

/**
 * Development-only gallery of every `ui/` component with its variants, in place of Storybook
 * (docs/decisions.md). Registered in app.routes.ts only in dev mode; excluded from coverage.
 */
@Component({
  selector: 'app-styleguide-page',
  imports: [UiButton, UiFactBar, UiSectionHeading],
  templateUrl: './styleguide-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StyleguidePage {
  protected readonly facts: Fact[] = [
    { value: '5', label: 'Jahre Entwicklung im Produktivbetrieb' },
    { value: '1,8', label: 'IHK-Abschluss Fachinformatiker AE' },
    { value: '17', label: 'selbst betriebene Docker-Stacks' },
    { value: 'C1', label: 'Englisch' },
  ];
}
