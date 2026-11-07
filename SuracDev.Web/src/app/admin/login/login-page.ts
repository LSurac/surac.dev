import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UiButton } from '../../ui/button/button';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { adminStrings } from '../admin-strings';
import { AdminSession } from '../core/admin-session';
import { toProblem } from '../forms/problem';

const TOO_MANY_REQUESTS = 429;

/** The admin login. The very first login sets the password (docs/security-and-privacy.md). */
@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, UiButton, UiErrorSummary, UiField],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage implements OnInit {
  private readonly session = inject(AdminSession);
  private readonly router = inject(Router);

  protected readonly strings = adminStrings.login;
  protected readonly isFirstLogin = signal(false);
  protected readonly busy = signal(false);
  protected readonly errors = signal<readonly string[]>([]);
  protected readonly heading = computed(() =>
    this.isFirstLogin() ? this.strings.firstHeading : this.strings.heading,
  );
  protected readonly submitLabel = computed(() =>
    this.isFirstLogin() ? this.strings.firstSubmit : this.strings.submit,
  );
  protected readonly autocomplete = computed(() =>
    this.isFirstLogin() ? 'new-password' : 'current-password',
  );
  protected readonly form = inject(NonNullableFormBuilder).group({ password: ['', Validators.required] });

  async ngOnInit(): Promise<void> {
    const state = await this.session.loginState();
    this.isFirstLogin.set(!state.hasPassword);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.errors.set([]);
    try {
      await this.session.login(this.form.getRawValue().password);
      await this.router.navigateByUrl('/admin');
    } catch (error) {
      const problem = toProblem(error, this.strings.failed);
      this.errors.set(problem.status === TOO_MANY_REQUESTS ? [this.strings.tooMany] : problem.messages);
    } finally {
      this.busy.set(false);
    }
  }
}
