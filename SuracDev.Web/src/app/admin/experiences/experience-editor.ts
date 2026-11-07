import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminExperience,
  AdminExperiencesClient,
  ExperienceForm,
  ExperienceTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated, linesToText, textToLines } from '../forms/lines';
import { formatPeriod } from '../forms/period';
import { SaveProblem, toProblem } from '../forms/problem';

type TextKey = 'roleTitle' | 'summary' | 'bullets' | 'stackLine';
type Selection = number | 'new' | null;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };

/** Career stations: the list on the left, the chosen or a new station on the right. */
@Component({
  selector: 'app-experience-editor',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './experience-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExperienceEditor implements OnInit {
  private readonly experiences = inject(AdminExperiencesClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.experiences;
  protected readonly textKeys: readonly TextKey[] = ['roleTitle', 'summary', 'bullets', 'stackLine'];
  protected readonly multiline: ReadonlySet<TextKey> = new Set<TextKey>(['summary', 'bullets']);
  protected readonly items = signal<readonly AdminExperience[]>([]);
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
      title: item.company ?? '',
      meta: formatPeriod(item.startDate, item.endDate, this.text.openEnd),
      muted: !item.isVisible,
    })),
  );

  protected readonly form = this.fb.group({
    company: '',
    location: '',
    startDate: '',
    endDate: '',
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

  protected hintFor(key: TextKey): string | null {
    return key === 'bullets' ? adminStrings.form.linesHint : null;
  }

  protected untranslated(key: TextKey): string | null {
    const value = this.form.getRawValue();
    return isUntranslated(value.de[key], value.en[key]) ? adminStrings.locale.untranslated : null;
  }

  protected select(id: number): void {
    const experience = this.items().find((item) => item.id === id);
    if (!experience) {
      return;
    }

    this.resetMessages();
    this.form.setValue({
      company: experience.company ?? '',
      location: experience.location ?? '',
      startDate: experience.startDate ?? '',
      endDate: experience.endDate ?? '',
      sortOrder: experience.sortOrder ?? 0,
      isVisible: experience.isVisible ?? true,
      de: toTextValue(experience.de),
      en: toTextValue(experience.en),
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
          ? (await firstValueFrom(this.experiences.create(this.toForm()))).id
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
      await firstValueFrom(this.experiences.delete(id));
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

    await firstValueFrom(this.experiences.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.experiences.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private toForm(): ExperienceForm {
    const value = this.form.getRawValue();
    return {
      company: value.company,
      location: value.location,
      startDate: value.startDate,
      endDate: value.endDate || undefined,
      sortOrder: value.sortOrder,
      isVisible: value.isVisible,
      de: toTexts(value.de),
      en: toTexts(value.en),
    };
  }

  private textGroup() {
    return this.fb.group({ roleTitle: '', summary: '', bullets: '', stackLine: '' });
  }
}

function toTextValue(texts: ExperienceTextsForm | undefined): Record<TextKey, string> {
  return {
    roleTitle: texts?.roleTitle ?? '',
    summary: texts?.summary ?? '',
    bullets: linesToText(texts?.bullets),
    stackLine: texts?.stackLine ?? '',
  };
}

function toTexts(value: Record<TextKey, string>): ExperienceTextsForm {
  return { ...value, bullets: textToLines(value.bullets) };
}
