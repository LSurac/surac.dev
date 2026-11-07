import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminLegalPage,
  AdminLegalPagesClient,
  AdminLegalTexts,
  LegalPageForm,
  LegalPageKey,
  LegalTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated } from '../forms/lines';
import { SaveProblem, toProblem } from '../forms/problem';

type TextKey = 'title' | 'bodyMarkdown';

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };

/** The two legal pages exist from the start and are never created or deleted. */
const PAGES: readonly { readonly key: LegalPageKey; readonly label: string }[] = [
  { key: 'Imprint', label: adminStrings.legal.imprint },
  { key: 'Privacy', label: adminStrings.legal.privacy },
];

/** Legal pages: pick Impressum or Datenschutz, edit both locales as Markdown. */
@Component({
  selector: 'app-legal-editor',
  imports: [DatePipe, ReactiveFormsModule, UiButton, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './legal-editor.html',
  styleUrls: ['../forms/editor.scss', './legal-editor.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalEditor implements OnInit {
  private readonly legalPages = inject(AdminLegalPagesClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.legal;
  protected readonly pageChoices = PAGES;
  protected readonly textKeys: readonly TextKey[] = ['title', 'bodyMarkdown'];
  protected readonly pages = signal<readonly AdminLegalPage[]>([]);
  protected readonly currentKey = signal<LegalPageKey>('Imprint');
  protected readonly loaded = signal(false);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly problem = signal<SaveProblem>(NO_PROBLEM);
  protected readonly loadFailed = computed(() => !this.loaded() && this.problem().messages.length > 0);
  protected readonly currentPage = computed(() =>
    this.pages().find((page) => page.key === this.currentKey()),
  );

  protected readonly form = this.fb.group({
    de: this.textGroup(),
    en: this.textGroup(),
  });

  async ngOnInit(): Promise<void> {
    try {
      this.pages.set(await firstValueFrom(this.legalPages.list()));
      this.fillCurrent();
      this.loaded.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected labelFor(key: TextKey): string {
    return this.text[key];
  }

  protected untranslated(key: TextKey): string | null {
    const value = this.form.getRawValue();
    return isUntranslated(value.de[key], value.en[key]) ? adminStrings.locale.untranslated : null;
  }

  protected select(key: LegalPageKey): void {
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
    this.currentKey.set(key);
    this.fillCurrent();
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
    try {
      await firstValueFrom(this.legalPages.update(this.currentKey(), this.toForm()));
      this.pages.set(await firstValueFrom(this.legalPages.list()));
      this.fillCurrent();
      this.saved.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    } finally {
      this.saving.set(false);
    }
  }

  private fillCurrent(): void {
    const page = this.currentPage();
    this.form.setValue({ de: toTextValue(page?.de), en: toTextValue(page?.en) });
  }

  private toForm(): LegalPageForm {
    const value = this.form.getRawValue();
    return { de: value.de, en: hasText(value.en) ? value.en : undefined };
  }

  private textGroup() {
    return this.fb.group({ title: '', bodyMarkdown: '' });
  }
}

function toTextValue(texts: AdminLegalTexts | undefined): Record<TextKey, string> {
  return { title: texts?.title ?? '', bodyMarkdown: texts?.bodyMarkdown ?? '' };
}

function hasText(texts: LegalTextsForm): boolean {
  return !!texts.title?.trim() || !!texts.bodyMarkdown?.trim();
}
