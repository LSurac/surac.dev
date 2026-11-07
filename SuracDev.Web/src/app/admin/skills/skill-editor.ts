import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormArray, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminSkillGroup,
  AdminSkillGroupsClient,
  SkillForm,
  SkillGroupForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated } from '../forms/lines';
import { SaveProblem, toProblem } from '../forms/problem';

type Selection = number | 'new' | null;
type SkillRow = ReturnType<typeof createSkillRow>;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };

/** The API reports skill errors per group, not per row, so they are listed above the rows. */
const SKILL_ERROR_KEYS = [
  'skills',
  'skills.name',
  'skills.sinceYear',
  'de.skills.qualifier',
  'en.skills.qualifier',
];

/** Skill groups: the list on the left, the chosen or a new group with its skills on the right. */
@Component({
  selector: 'app-skill-editor',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './skill-editor.html',
  styleUrls: ['../forms/editor.scss', './skill-editor.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillEditor implements OnInit {
  private readonly groups = inject(AdminSkillGroupsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.skills;
  protected readonly items = signal<readonly AdminSkillGroup[]>([]);
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
      title: item.nameDe ?? '',
      meta: this.countLabel(item.skills?.length ?? 0),
    })),
  );
  protected readonly skillMessages = computed(() =>
    SKILL_ERROR_KEYS.flatMap((key) => this.problem().fieldErrors[key] ?? []),
  );

  protected readonly form = this.fb.group({
    sortOrder: 0,
    nameDe: '',
    nameEn: '',
    skills: this.fb.array<SkillRow>([]),
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  protected get skills(): FormArray<SkillRow> {
    return this.form.controls.skills;
  }

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected untranslated(): string | null {
    const value = this.form.getRawValue();
    return isUntranslated(value.nameDe, value.nameEn) ? adminStrings.locale.untranslated : null;
  }

  protected skillLabel(action: string, index: number): string {
    return `${action}: ${this.skills.at(index).value.name}`;
  }

  protected addSkill(): void {
    this.skills.push(createSkillRow(this.fb));
  }

  protected removeSkill(index: number): void {
    this.skills.removeAt(index);
  }

  protected moveSkill(index: number, offset: -1 | 1): void {
    const target = index + offset;
    if (target < 0 || target >= this.skills.length) {
      return;
    }

    const row = this.skills.at(index);
    this.skills.removeAt(index);
    this.skills.insert(target, row);
  }

  protected select(id: number): void {
    const group = this.items().find((item) => item.id === id);
    if (!group) {
      return;
    }

    this.resetMessages();
    const skills = group.skills ?? [];
    this.skills.clear();
    skills.forEach(() => this.skills.push(createSkillRow(this.fb)));
    this.form.setValue({
      sortOrder: group.sortOrder ?? 0,
      nameDe: group.nameDe ?? '',
      nameEn: group.nameEn ?? '',
      skills: skills.map((skill) => ({
        name: skill.name ?? '',
        sinceYear: skill.sinceYear ?? null,
        qualifierDe: skill.qualifierDe ?? '',
        qualifierEn: skill.qualifierEn ?? '',
      })),
    });
    this.selection.set(id);
  }

  protected startNew(): void {
    this.resetMessages();
    this.skills.clear();
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
          ? (await firstValueFrom(this.groups.create(this.toForm()))).id
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
      await firstValueFrom(this.groups.delete(id));
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

    await firstValueFrom(this.groups.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.groups.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private countLabel(count: number): string {
    return `${count} ${count === 1 ? this.text.countOne : this.text.countMany}`;
  }

  private toForm(): SkillGroupForm {
    const value = this.form.getRawValue();
    return {
      sortOrder: value.sortOrder,
      nameDe: value.nameDe,
      nameEn: value.nameEn || undefined,
      skills: value.skills.map((skill): SkillForm => ({
        name: skill.name,
        sinceYear: skill.sinceYear ?? undefined,
        qualifierDe: skill.qualifierDe || undefined,
        qualifierEn: skill.qualifierEn || undefined,
      })),
    };
  }
}

function createSkillRow(fb: NonNullableFormBuilder) {
  return fb.group({
    name: '',
    sinceYear: fb.control<number | null>(null),
    qualifierDe: '',
    qualifierEn: '',
  });
}
