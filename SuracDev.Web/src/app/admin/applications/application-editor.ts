import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminApplication,
  AdminApplicationsClient,
  ApplicationForm,
  ApplicationStatus,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { EntityListItem, UiEntityList } from '../../ui/entity-list/entity-list';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { adminStrings } from '../admin-strings';
import { SaveProblem, toProblem } from '../forms/problem';
import { ApplicationLinks } from './application-links';

type Selection = number | 'new' | null;
type KnownStatus = Exclude<ApplicationStatus, 'Unknown'>;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };
const STATUSES = Object.keys(adminStrings.applications.statuses) as KnownStatus[];

/**
 * The mini ATS: applications on the left, the chosen or a new application on the right, and below
 * it the links created for it (docs/content-model.md, "Application and ApplicationLink").
 */
@Component({
  selector: 'app-application-editor',
  imports: [ApplicationLinks, ReactiveFormsModule, UiButton, UiEntityList, UiErrorSummary, UiField],
  templateUrl: './application-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationEditor implements OnInit {
  private readonly applications = inject(AdminApplicationsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.applications;
  protected readonly statuses = STATUSES;
  protected readonly items = signal<readonly AdminApplication[]>([]);
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
      meta: `${item.position ?? ''} · ${this.statusLabel(item.status)}`,
    })),
  );

  protected readonly form = this.fb.group({
    company: '',
    position: '',
    jobAdUrl: '',
    appliedOn: '',
    status: 'Draft' as KnownStatus,
    notes: '',
  });

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected statusLabel(status: ApplicationStatus | undefined): string {
    return STATUSES.includes(status as KnownStatus) ? this.text.statuses[status as KnownStatus] : '';
  }

  protected select(id: number): void {
    const application = this.items().find((item) => item.id === id);
    if (!application) {
      return;
    }

    this.resetMessages();
    this.form.setValue({
      company: application.company ?? '',
      position: application.position ?? '',
      jobAdUrl: application.jobAdUrl ?? '',
      appliedOn: application.appliedOn ?? '',
      status: (application.status as KnownStatus | undefined) ?? 'Draft',
      notes: application.notes ?? '',
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
          ? (await firstValueFrom(this.applications.create(this.toForm()))).id
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
      await firstValueFrom(this.applications.delete(id));
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

    await firstValueFrom(this.applications.update(id, this.toForm()));
    return id;
  }

  private async reload(): Promise<void> {
    try {
      this.items.set(await firstValueFrom(this.applications.list()));
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  private resetMessages(): void {
    this.confirmingDelete.set(false);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
  }

  private toForm(): ApplicationForm {
    const value = this.form.getRawValue();
    return {
      company: value.company,
      position: value.position,
      jobAdUrl: value.jobAdUrl || undefined,
      appliedOn: value.appliedOn || undefined,
      status: value.status,
      notes: value.notes || undefined,
    };
  }
}
