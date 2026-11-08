import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminDocument,
  AdminDocumentsClient,
  DocumentKind,
  DocumentVisibility,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { adminStrings } from '../admin-strings';
import { isPublicImage } from '../forms/public-images';
import { SaveProblem, toProblem } from '../forms/problem';

type Selection = number | 'upload' | null;
type KnownKind = keyof typeof adminStrings.documents.kinds;
type KnownVisibility = keyof typeof adminStrings.documents.visibilities;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };
const KINDS: readonly KnownKind[] = ['Photo', 'Image', 'Certificate', 'CoverLetter', 'Other'];
const VISIBILITIES: readonly KnownVisibility[] = ['Public', 'LinkOnly'];

/**
 * Uploaded documents: upload with kind, visibility and labels, preview of public images, download,
 * relabel, change visibility and delete. The API enforces what may be public and what is in use;
 * this screen shows its answers.
 */
@Component({
  selector: 'app-document-manager',
  imports: [ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField],
  templateUrl: './document-manager.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentManager implements OnInit {
  private readonly documents = inject(AdminDocumentsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.documents;
  protected readonly kinds = KINDS;
  protected readonly visibilities = VISIBILITIES;
  protected readonly items = signal<readonly AdminDocument[]>([]);
  protected readonly selection = signal<Selection>(null);
  protected readonly file = signal<File | null>(null);
  protected readonly busy = signal(false);
  protected readonly saved = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly problem = signal<SaveProblem>(NO_PROBLEM);
  protected readonly selectedId = computed(() => {
    const selection = this.selection();
    return typeof selection === 'number' ? selection : null;
  });
  protected readonly selected = computed(
    () => this.items().find((item) => item.id === this.selectedId()) ?? null,
  );
  protected readonly listItems = computed<EntityListItem[]>(() =>
    this.items().map((item) => ({
      id: item.id ?? 0,
      title: item.labelDe || item.fileName || '',
      meta: `${this.kindLabel(item.kind)} · ${this.visibilityLabel(item.visibility)}`,
    })),
  );

  protected readonly uploadForm = this.fb.group({
    kind: this.fb.control<KnownKind>('Image'),
    visibility: this.fb.control<KnownVisibility>('Public'),
    labelDe: '',
    labelEn: '',
  });

  protected readonly editForm = this.fb.group({
    visibility: this.fb.control<KnownVisibility>('Public'),
    labelDe: '',
    labelEn: '',
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  protected kindLabel(kind: DocumentKind | undefined): string {
    return adminStrings.documents.kinds[kind as KnownKind] ?? kind ?? '';
  }

  protected visibilityLabel(visibility: DocumentVisibility | undefined): string {
    return adminStrings.documents.visibilities[visibility as KnownVisibility] ?? visibility ?? '';
  }

  protected isPublicImage(document: AdminDocument): boolean {
    return isPublicImage(document);
  }

  protected sizeLabel(document: AdminDocument): string {
    return `${((document.sizeBytes ?? 0) / 1024).toFixed(0)} KB`;
  }

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected select(id: number): void {
    const document = this.items().find((item) => item.id === id);
    if (!document) {
      return;
    }

    this.resetMessages();
    this.editForm.setValue({
      visibility: (document.visibility as KnownVisibility) ?? 'Public',
      labelDe: document.labelDe ?? '',
      labelEn: document.labelEn ?? '',
    });
    this.selection.set(id);
  }

  protected startUpload(): void {
    this.resetMessages();
    this.uploadForm.reset();
    this.file.set(null);
    this.selection.set('upload');
  }

  protected chooseFile(event: Event): void {
    this.file.set((event.target as HTMLInputElement).files?.item(0) ?? null);
  }

  protected async upload(): Promise<void> {
    const file = this.file();
    this.resetMessages();
    if (!file) {
      this.problem.set({ status: null, messages: [adminStrings.documents.noFile], fieldErrors: {} });
      return;
    }

    const value = this.uploadForm.getRawValue();
    await this.run(async () => {
      const created = await firstValueFrom(
        this.documents.upload(
          { data: file, fileName: file.name },
          value.kind,
          value.visibility,
          value.labelDe,
          value.labelEn,
        ),
      );
      await this.reload();
      if (created.id !== undefined) {
        this.select(created.id);
      }
    });
  }

  protected async save(): Promise<void> {
    const id = this.selectedId();
    if (id === null) {
      return;
    }

    this.resetMessages();
    await this.run(async () => {
      await firstValueFrom(this.documents.update(id, this.editForm.getRawValue()));
      await this.reload();
      this.select(id);
    });
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

    await this.run(async () => {
      await firstValueFrom(this.documents.delete(id));
      this.selection.set(null);
      await this.reload();
    });
  }

  /** Runs one request; success shows "saved", a failure shows the API's answer. */
  private async run(request: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    try {
      await request();
      this.saved.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    } finally {
      this.busy.set(false);
    }
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.documents.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }
}
