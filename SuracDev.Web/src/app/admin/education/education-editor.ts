import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminEducation,
  AdminEducationClient,
  EducationForm,
  EducationTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated } from '../forms/lines';
import { formatPeriod } from '../../core/format/period';
import { SaveProblem, toProblem } from '../forms/problem';

type TextKey = 'degree' | 'summary';
type Selection = number | 'new' | null;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };

/** Education entries: the list on the left, the chosen or a new entry on the right. */
@Component({
  selector: 'app-education-editor',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './education-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationEditor implements OnInit {
  private readonly education = inject(AdminEducationClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.education;
  protected readonly textKeys: readonly TextKey[] = ['degree', 'summary'];
  protected readonly multiline: ReadonlySet<TextKey> = new Set<TextKey>(['summary']);
  protected readonly items = signal<readonly AdminEducation[]>([]);
  protected readonly selection = signal<Selection>(null);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly problem = signal<SaveProblem>(NO_PROBLEM);
  protected readonly selectedId = computed(() => {
    const selection = this.selection();
    return typeof selection === 'number' ? selection : null;
  });
  protected readonly listItems = computed<EntityListItem[]>(() =>
    this.items().map((item) => ({
      id: item.id ?? 0,
      title: item.institution ?? '',
      meta: formatPeriod(item.startDate, item.endDate, this.text.openEnd),
      muted: !item.isVisible,
    })),
  );

  protected readonly form = this.fb.group({
    institution: '',
    location: '',
    startDate: '',
    endDate: '',
    grade: '',
    sortOrder: 0,
    isVisible: true,
    de: this.textGroup(),
    en: this.textGroup(),
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
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

  protected select(id: number): void {
    const entry = this.items().find((item) => item.id === id);
    if (!entry) {
      return;
    }

    this.resetMessages();
    this.form.setValue({
      institution: entry.institution ?? '',
      location: entry.location ?? '',
      startDate: entry.startDate ?? '',
      endDate: entry.endDate ?? '',
      grade: entry.grade ?? '',
      sortOrder: entry.sortOrder ?? 0,
      isVisible: entry.isVisible ?? true,
      de: toTextValue(entry.de),
      en: toTextValue(entry.en),
    });
    this.selection.set(id);
  }

  protected startNew(): void {
    this.resetMessages();
    this.form.reset();
    this.selection.set('new');
  }

  protected async save(): Promise<void> {
    const selection = this.selection();
    this.saving.set(true);
    this.resetMessages();
    try {
      const id =
        selection === 'new'
          ? (await firstValueFrom(this.education.create(this.toForm()))).id
          : await this.update(selection);
      await this.reload();
      if (id !== undefined && id !== null) {
        this.select(id);
      }
      this.saved.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    } finally {
      this.saving.set(false);
    }
  }

  protected askToDelete(): void {
    this.confirmingDelete.set(true);
  }

  protected cancelDelete(): void {
    this.confirmingDelete.set(false);
  }

  protected async remove(): Promise<void> {
    const id = this.selectedId();
    this.confirmingDelete.set(false);
    if (id === null) {
      return;
    }

    try {
      await firstValueFrom(this.education.delete(id));
      this.selection.set(null);
      await this.reload();
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    }
  }

  private async update(id: number | null): Promise<number | null> {
    if (id === null) {
      return null;
    }

    await firstValueFrom(this.education.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.education.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private toForm(): EducationForm {
    const value = this.form.getRawValue();
    return {
      institution: value.institution,
      location: value.location,
      startDate: value.startDate,
      endDate: value.endDate || undefined,
      grade: value.grade,
      sortOrder: value.sortOrder,
      isVisible: value.isVisible,
      de: value.de,
      en: value.en,
    };
  }

  private textGroup() {
    return this.fb.group({ degree: '', summary: '' });
  }
}

function toTextValue(texts: EducationTextsForm | undefined): Record<TextKey, string> {
  return { degree: texts?.degree ?? '', summary: texts?.summary ?? '' };
}
