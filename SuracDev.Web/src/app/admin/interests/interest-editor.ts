import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminDocument,
  AdminDocumentsClient,
  AdminInterest,
  AdminInterestsClient,
  InterestForm,
  InterestKind,
  InterestTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated } from '../forms/lines';
import { SaveProblem, toProblem } from '../forms/problem';
import { imageOptionLabel, isPublicImage } from '../forms/public-images';

type TextKey = 'title' | 'body';
type Selection = number | 'new' | null;
type KnownKind = Exclude<InterestKind, 'Unknown'>;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };
const KINDS = Object.keys(adminStrings.interests.kinds) as KnownKind[];

/** The sections of the "Über mich" page: the list on the left, the chosen or a new section on the right. */
@Component({
  selector: 'app-interest-editor',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './interest-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InterestEditor implements OnInit {
  private readonly interests = inject(AdminInterestsClient);
  private readonly documents = inject(AdminDocumentsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.interests;
  protected readonly kinds = KINDS;
  protected readonly textKeys: readonly TextKey[] = ['title', 'body'];
  protected readonly items = signal<readonly AdminInterest[]>([]);
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
      title: item.de?.title ?? '',
      meta: this.text.kinds[item.kind as KnownKind] ?? '',
      muted: !item.isVisible,
    })),
  );

  protected readonly form = this.fb.group({
    kind: 'Other' as KnownKind,
    sortOrder: 0,
    isVisible: true,
    imageDocumentId: this.fb.control<number | null>(null),
    de: this.textGroup(),
    en: this.textGroup(),
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
    try {
      this.images.set((await firstValueFrom(this.documents.list())).filter(isPublicImage));
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

  protected hintFor(key: TextKey): string | null {
    return key === 'body' ? this.text.bodyHint : null;
  }

  protected imageLabel(document: AdminDocument): string {
    return imageOptionLabel(document);
  }

  protected untranslated(key: TextKey): string | null {
    const value = this.form.getRawValue();
    return isUntranslated(value.de[key], value.en[key]) ? adminStrings.locale.untranslated : null;
  }

  protected select(id: number): void {
    const interest = this.items().find((item) => item.id === id);
    if (!interest) {
      return;
    }

    this.resetMessages();
    this.form.setValue({
      kind: (interest.kind as KnownKind | undefined) ?? 'Other',
      sortOrder: interest.sortOrder ?? 0,
      isVisible: interest.isVisible ?? true,
      imageDocumentId: interest.imageDocumentId ?? null,
      de: toTextValue(interest.de),
      en: toTextValue(interest.en),
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
          ? (await firstValueFrom(this.interests.create(this.toForm()))).id
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
      await firstValueFrom(this.interests.delete(id));
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

    await firstValueFrom(this.interests.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.interests.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private toForm(): InterestForm {
    const value = this.form.getRawValue();
    return {
      kind: value.kind,
      sortOrder: value.sortOrder,
      isVisible: value.isVisible,
      imageDocumentId: value.imageDocumentId ?? undefined,
      de: value.de,
      en: value.en,
    };
  }

  private textGroup() {
    return this.fb.group({ title: '', body: '' });
  }
}

function toTextValue(texts: InterestTextsForm | undefined): Record<TextKey, string> {
  return { title: texts?.title ?? '', body: texts?.body ?? '' };
}
