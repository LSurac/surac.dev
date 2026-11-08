import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormArray, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminDocument,
  AdminDocumentsClient,
  AdminProject,
  AdminProjectsClient,
  DecisionForm,
  ImageForm,
  ProjectForm,
  ProjectStatus,
  ProjectTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { imageOptionLabel, isPublicImage } from '../forms/public-images';
import { isUntranslated, linesToText, textToLines } from '../forms/lines';
import { SaveProblem, toProblem } from '../forms/problem';

type TextKey = keyof ProjectTextsForm;
type Selection = number | 'new' | null;
type DecisionGroup = ReturnType<typeof createDecisionGroup>;
type ImageGroup = ReturnType<typeof createImageGroup>;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };
const STATUSES: readonly ProjectStatus[] = ['Live', 'InProduction', 'Archived', 'Planned'];
const TEXT_KEYS: readonly TextKey[] = [
  'title',
  'teaser',
  'problem',
  'myRole',
  'outcome',
  'caseStudyMarkdown',
];

/** Projects with their case study, decisions and images; list on the left, form on the right. */
@Component({
  selector: 'app-project-editor',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './project-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectEditor implements OnInit {
  private readonly projects = inject(AdminProjectsClient);
  private readonly documents = inject(AdminDocumentsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.projects;
  protected readonly statuses = STATUSES;
  protected readonly textKeys = TEXT_KEYS;
  protected readonly multiline: ReadonlySet<TextKey> = new Set<TextKey>([
    'problem',
    'myRole',
    'outcome',
    'caseStudyMarkdown',
  ]);
  protected readonly items = signal<readonly AdminProject[]>([]);
  protected readonly images = signal<readonly AdminDocument[]>([]);
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
      title: item.de?.title ?? item.slug ?? '',
      meta: item.isFeatured ? `${this.text.onStartPage} · ${item.slug}` : item.slug,
      muted: !item.isVisible,
    })),
  );

  protected readonly form = this.fb.group({
    slug: '',
    status: this.fb.control<ProjectStatus>('Live'),
    startedYear: this.fb.control<number | null>(null),
    endedYear: this.fb.control<number | null>(null),
    liveUrl: '',
    repositoryUrl: '',
    stackTags: '',
    sortOrder: 0,
    isFeatured: false,
    isVisible: true,
    de: this.textGroup(),
    en: this.textGroup(),
    decisions: this.fb.array<DecisionGroup>([]),
    images: this.fb.array<ImageGroup>([]),
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
    try {
      const documents = await firstValueFrom(this.documents.list());
      this.images.set(documents.filter(isPublicImage));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  protected get decisions(): FormArray<DecisionGroup> {
    return this.form.controls.decisions;
  }

  protected get imageRows(): FormArray<ImageGroup> {
    return this.form.controls.images;
  }

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected labelFor(key: TextKey): string {
    return this.text[key];
  }

  protected hintFor(key: TextKey): string | null {
    return key === 'caseStudyMarkdown' ? this.text.markdownHint : null;
  }

  protected statusLabel(status: ProjectStatus): string {
    return this.text.statuses[status as keyof typeof adminStrings.projects.statuses] ?? status;
  }

  protected imageLabel(document: AdminDocument): string {
    return imageOptionLabel(document);
  }

  protected untranslated(key: TextKey): string | null {
    const value = this.form.getRawValue();
    return isUntranslated(value.de[key], value.en[key]) ? adminStrings.locale.untranslated : null;
  }

  protected removeDecisionLabel(index: number): string {
    return `${adminStrings.form.remove}: ${this.decisions.at(index).value.titleDe ?? ''}`;
  }

  protected removeImageLabel(index: number): string {
    return `${adminStrings.form.remove}: ${this.text.image} ${index + 1}`;
  }

  protected addDecision(): void {
    this.decisions.push(createDecisionGroup(this.fb));
  }

  protected removeDecision(index: number): void {
    this.decisions.removeAt(index);
  }

  protected addImage(): void {
    const image = createImageGroup(this.fb);
    image.controls.documentId.setValue(this.images()[0]?.id ?? null);
    this.imageRows.push(image);
  }

  protected removeImage(index: number): void {
    this.imageRows.removeAt(index);
  }

  protected select(id: number): void {
    const project = this.items().find((item) => item.id === id);
    if (!project) {
      return;
    }

    this.resetMessages();
    this.fill(project);
    this.selection.set(id);
  }

  protected startNew(): void {
    this.resetMessages();
    this.decisions.clear();
    this.imageRows.clear();
    this.form.reset();
    this.selection.set('new');
  }

  protected async save(): Promise<void> {
    const selection = this.selection();
    this.saving.set(true);
    this.resetMessages();
    try {
      const id = selection === 'new' ? await this.create() : await this.update(selection);
      await this.reload();
      if (id !== null) {
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
      await firstValueFrom(this.projects.delete(id));
      this.selection.set(null);
      await this.reload();
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    }
  }

  private async create(): Promise<number | null> {
    const created = await firstValueFrom(this.projects.create(this.toForm()));
    return created.id ?? null;
  }

  private async update(id: number | null): Promise<number | null> {
    if (id === null) {
      return null;
    }

    await firstValueFrom(this.projects.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.projects.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private fill(project: AdminProject): void {
    this.decisions.clear();
    (project.decisions ?? []).forEach(() => this.decisions.push(createDecisionGroup(this.fb)));
    this.imageRows.clear();
    (project.images ?? []).forEach(() => this.imageRows.push(createImageGroup(this.fb)));

    this.form.setValue({
      slug: project.slug ?? '',
      status: project.status ?? 'Live',
      startedYear: project.startedYear ?? null,
      endedYear: project.endedYear ?? null,
      liveUrl: project.liveUrl ?? '',
      repositoryUrl: project.repositoryUrl ?? '',
      stackTags: linesToText(project.stackTags),
      sortOrder: project.sortOrder ?? 0,
      isFeatured: project.isFeatured ?? false,
      isVisible: project.isVisible ?? true,
      de: toTextValue(project.de),
      en: toTextValue(project.en),
      decisions: (project.decisions ?? []).map((decision) => ({
        titleDe: decision.de?.title ?? '',
        tradeOffDe: decision.de?.tradeOff ?? '',
        titleEn: decision.en?.title ?? '',
        tradeOffEn: decision.en?.tradeOff ?? '',
      })),
      images: (project.images ?? []).map((image) => ({
        documentId: image.documentId ?? null,
        captionDe: image.captionDe ?? '',
        captionEn: image.captionEn ?? '',
      })),
    });
  }

  private toForm(): ProjectForm {
    const value = this.form.getRawValue();
    return {
      slug: value.slug,
      status: value.status,
      startedYear: value.startedYear ?? undefined,
      endedYear: value.endedYear ?? undefined,
      liveUrl: value.liveUrl,
      repositoryUrl: value.repositoryUrl,
      stackTags: textToLines(value.stackTags),
      sortOrder: value.sortOrder,
      isFeatured: value.isFeatured,
      isVisible: value.isVisible,
      de: value.de,
      en: value.en,
      decisions: value.decisions.map(toDecision),
      images: value.images.map(toImage),
    };
  }

  private textGroup() {
    return this.fb.group({
      title: '',
      teaser: '',
      problem: '',
      myRole: '',
      outcome: '',
      caseStudyMarkdown: '',
    });
  }
}

function createDecisionGroup(fb: NonNullableFormBuilder) {
  return fb.group({ titleDe: '', tradeOffDe: '', titleEn: '', tradeOffEn: '' });
}

function createImageGroup(fb: NonNullableFormBuilder) {
  return fb.group({ documentId: fb.control<number | null>(null), captionDe: '', captionEn: '' });
}

function toTextValue(texts: ProjectTextsForm | undefined): Record<TextKey, string> {
  return {
    title: texts?.title ?? '',
    teaser: texts?.teaser ?? '',
    problem: texts?.problem ?? '',
    myRole: texts?.myRole ?? '',
    outcome: texts?.outcome ?? '',
    caseStudyMarkdown: texts?.caseStudyMarkdown ?? '',
  };
}

function toDecision(row: DecisionGroup['value']): DecisionForm {
  const hasEnglish = !!row.titleEn?.trim() || !!row.tradeOffEn?.trim();
  return {
    de: { title: row.titleDe, tradeOff: row.tradeOffDe },
    en: hasEnglish ? { title: row.titleEn, tradeOff: row.tradeOffEn } : undefined,
  };
}

function toImage(row: ImageGroup['value']): ImageForm {
  return { documentId: row.documentId ?? undefined, captionDe: row.captionDe, captionEn: row.captionEn };
}
