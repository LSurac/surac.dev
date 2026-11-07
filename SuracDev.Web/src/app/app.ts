import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { uiStrings } from './core/i18n/ui-strings';
import { LocaleService } from './core/locale/locale.service';
import { SiteFooter } from './layout/site-footer/site-footer';
import { SiteHeader } from './layout/site-header/site-header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeader, SiteFooter],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly localeService = inject(LocaleService);

  protected readonly locale = this.localeService.locale;
  protected readonly strings = computed(() => uiStrings(this.locale()));
}
