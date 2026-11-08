import { ChangeDetectionStrategy, Component, computed, DOCUMENT, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminApplicationLink,
  AdminApplicationLinksClient,
  AdminDocumentsClient,
  ApplicationLinkForm,
  AudienceProfile,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { adminStrings } from '../admin-strings';
import { SaveProblem, toProblem } from '../forms/problem';

type KnownProfile = Exclude<AudienceProfile, 'Unknown'>;
type Editing = number | 'new' | null;

/** A link prepared for the list: one summary line and whether it can still be changed. */
interface LinkRow {
  readonly id: number;
  readonly summary: string;
  readonly isRevoked: boolean;
}

/** The addresses of a freshly created link, shown once because only the token's hash is stored. */
interface CreatedAddresses {
  readonly de: string;
  readonly en: string;
}

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };
const PROFILES = Object.keys(adminStrings.links.profiles) as KnownProfile[];
const DATE_FORMAT = new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: '2-digit', year: 'numeric' });

/**
 * The links of one application: list, create (the only moment the address can be copied), edit
 * and revoke. Only link-only documents are offered for attaching (docs/security-and-privacy.md).
 */
@Component({
  selector: 'app-application-links',
  imports: [ReactiveFormsModule, UiButton, UiErrorSummary, UiField],
  templateUrl: './application-links.html',
  styleUrls: ['../forms/editor.scss', './application-links.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationLinks {
  private readonly links = inject(AdminApplicationLinksClient);
  private readonly documents = inject(AdminDocumentsClient);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly origin = inject(DOCUMENT).location.origin;

  readonly applicationId = input.required<number>();

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.links;
  protected readonly profiles = PROFILES;
  protected readonly editing = signal<Editing>(null);
  protected readonly attached = signal<readonly number[]>([]);
  protected readonly created = signal<CreatedAddresses | null>(null);
  protected readonly confirmingRevoke = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly problem = signal<SaveProblem>(NO_PROBLEM);

  private readonly linksResource = rxResource({
    params: () => this.applicationId(),
    stream: ({ params }) => this.links.list(params),
  });
  private readonly documentsResource = rxResource({ stream: () => this.documents.list() });

  protected readonly rows = computed<LinkRow[]>(() =>
    (this.linksResource.hasValue() ? this.linksResource.value() : []).map((link) => ({
      id: link.id ?? 0,
      summary: summarize(link),
      isRevoked: !!link.isRevoked,
    })),
  );
  protected readonly linkOnlyDocuments = computed(() =>
    (this.documentsResource.hasValue() ? this.documentsResource.value() : [])
      .filter((document) => document.visibility === 'LinkOnly')
      .map((document) => ({ id: document.id ?? 0, label: document.labelDe ?? document.fileName ?? '' })),
  );

  protected readonly form = this.fb.group({
    profile: 'Recruiter' as KnownProfile,
    greetingDe: '',
    greetingEn: '',
    expiresAt: '',
    unlockFullPdf: false,
  });

  protected errorsFor(key: string): readonly string[] {
    return this.problem().fieldErrors[key] ?? [];
  }

  protected isAttached(documentId: number): boolean {
    return this.attached().includes(documentId);
  }

  protected toggleDocument(documentId: number): void {
    this.attached.update((ids) =>
      ids.includes(documentId) ? ids.filter((id) => id !== documentId) : [...ids, documentId],
    );
  }

  protected startNew(): void {
    this.resetMessages();
    this.form.reset();
    this.attached.set([]);
    this.editing.set('new');
  }

  protected edit(id: number): void {
    const link = this.linksResource.value()?.find((item) => item.id === id);
    if (!link) {
      return;
    }

    this.resetMessages();
    this.form.setValue({
      profile: (link.profile as KnownProfile | undefined) ?? 'Recruiter',
      greetingDe: link.greetingDe ?? '',
      greetingEn: link.greetingEn ?? '',
      expiresAt: link.expiresAt ? toDateInput(link.expiresAt) : '',
      unlockFullPdf: !!link.unlockFullPdf,
    });
    this.attached.set(link.documentIds ?? []);
    this.editing.set(id);
  }

  protected async save(): Promise<void> {
    const editing = this.editing();
    this.saving.set(true);
    this.resetMessages();
    try {
      if (editing === 'new') {
        const result = await firstValueFrom(this.links.create(this.applicationId(), this.toForm()));
        this.created.set(this.addressesOf(result.token ?? ''));
      } else if (editing !== null) {
        await firstValueFrom(this.links.update(this.applicationId(), editing, this.toForm()));
      }
      this.editing.set(null);
      this.linksResource.reload();
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    } finally {
      this.saving.set(false);
    }
  }

  protected cancel(): void {
    this.resetMessages();
    this.editing.set(null);
  }

  protected askToRevoke(id: number): void {
    this.confirmingRevoke.set(id);
  }

  protected cancelRevoke(): void {
    this.confirmingRevoke.set(null);
  }

  protected async revoke(id: number): Promise<void> {
    this.confirmingRevoke.set(null);
    try {
      await firstValueFrom(this.links.revoke(this.applicationId(), id));
      this.linksResource.reload();
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    }
  }

  private resetMessages(): void {
    this.created.set(null);
    this.confirmingRevoke.set(null);
    this.problem.set(NO_PROBLEM);
  }

  private addressesOf(token: string): CreatedAddresses {
    return { de: `${this.origin}/b/${token}`, en: `${this.origin}/en/b/${token}` };
  }

  private toForm(): ApplicationLinkForm {
    const value = this.form.getRawValue();
    return {
      profile: value.profile,
      greetingDe: value.greetingDe || undefined,
      greetingEn: value.greetingEn || undefined,
      expiresAt: value.expiresAt ? endOfDay(value.expiresAt) : undefined,
      unlockFullPdf: value.unlockFullPdf,
      documentIds: [...this.attached()],
    };
  }
}

function summarize(link: AdminApplicationLink): string {
  const text = adminStrings.links;
  const profile = text.profiles[link.profile as KnownProfile] ?? '';
  if (link.isRevoked) {
    return `${profile} · ${text.states.revoked}`;
  }

  const parts = [
    profile,
    link.isActive ? text.states.active : text.states.expired,
    `${text.until} ${DATE_FORMAT.format(new Date(link.expiresAt ?? ''))}`,
  ];
  if (link.unlockFullPdf) {
    parts.push(text.fullPdf);
  }
  if ((link.documentIds ?? []).length > 0) {
    parts.push(`${link.documentIds!.length} ${text.documentCount}`);
  }
  return parts.join(' · ');
}

/** `YYYY-MM-DD` of an instant in the browser's time zone, for a date input. */
function toDateInput(instant: string): string {
  const date = new Date(instant);
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** The last second of a chosen day in the browser's time zone, so a link works through that day. */
function endOfDay(day: string): string {
  return new Date(`${day}T23:59:59`).toISOString();
}
