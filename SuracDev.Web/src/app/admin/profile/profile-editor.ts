import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormArray, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import {
  AdminDocument,
  AdminDocumentsClient,
  AdminProfile,
  AdminProfileClient,
  FactForm,
  ProfileTextsForm,
} from '../../api/generated/admin.client';
import { UiButton } from '../../ui/button/button';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { UiField } from '../../ui/field/field';
import { UiLocalePair } from '../../ui/locale-pair/locale-pair';
import { adminStrings } from '../admin-strings';
import { isUntranslated, linesToText, textToLines } from '../forms/lines';
import { SaveProblem, toProblem } from '../forms/problem';

type TextKey = keyof ProfileTextsForm;
type FactGroup = ReturnType<typeof createFactGroup>;

const NO_PROBLEM: SaveProblem = { status: null, messages: [], fieldErrors: {} };

/** The profile editor: contact data, both locales side by side, the fact bar and the photo. */
@Component({
  selector: 'app-profile-editor',
  imports: [ReactiveFormsModule, UiButton, UiErrorSummary, UiField, UiLocalePair],
  templateUrl: './profile-editor.html',
  styleUrl: '../forms/editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileEditor implements OnInit {
  private readonly profiles = inject(AdminProfileClient);
  private readonly documents = inject(AdminDocumentsClient);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.profile;
  protected readonly textKeys: readonly TextKey[] = ['titleLine', 'headline', 'intro', 'availabilityLine'];
  protected readonly multiline: ReadonlySet<TextKey> = new Set<TextKey>(['intro']);
  protected readonly loaded = signal(false);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly problem = signal<SaveProblem>(NO_PROBLEM);
  protected readonly photos = signal<readonly AdminDocument[]>([]);
  protected readonly loadFailed = computed(() => !this.loaded() && this.problem().messages.length > 0);

  protected readonly form = this.fb.group({
    fullName: '',
    email: '',
    phone: '',
    location: '',
    addressLines: '',
    gitHubUrl: '',
    linkedInUrl: '',
    photoDocumentId: this.fb.control<number | null>(null),
    de: this.textGroup(),
    en: this.textGroup(),
    facts: this.fb.array<FactGroup>([]),
  });

  async ngOnInit(): Promise<void> {
    try {
      const [profile, documents] = await Promise.all([
        firstValueFrom(this.profiles.get()),
        firstValueFrom(this.documents.list()),
      ]);
      this.photos.set(documents.filter((document) => isPublicImage(document)));
      this.fill(profile);
      this.loaded.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.loadFailed));
    }
  }

  protected get facts(): FormArray<FactGroup> {
    return this.form.controls.facts;
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

  protected photoLabel(document: AdminDocument): string {
    return document.labelDe ? `${document.labelDe} (${document.fileName})` : (document.fileName ?? '');
  }

  protected removeLabel(index: number): string {
    return `${adminStrings.form.remove}: ${this.facts.at(index).value.value}`;
  }

  protected addFact(): void {
    this.facts.push(createFactGroup(this.fb));
  }

  protected removeFact(index: number): void {
    this.facts.removeAt(index);
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.saved.set(false);
    this.problem.set(NO_PROBLEM);
    try {
      await firstValueFrom(this.profiles.put(this.toProfile()));
      this.saved.set(true);
    } catch (error) {
      this.problem.set(toProblem(error, adminStrings.form.unexpected));
    } finally {
      this.saving.set(false);
    }
  }

  private fill(profile: AdminProfile): void {
    this.facts.clear();
    (profile.facts ?? []).forEach(() => this.facts.push(createFactGroup(this.fb)));

    this.form.setValue({
      fullName: profile.fullName ?? '',
      email: profile.email ?? '',
      phone: profile.phone ?? '',
      location: profile.location ?? '',
      addressLines: linesToText(profile.addressLines),
      gitHubUrl: profile.gitHubUrl ?? '',
      linkedInUrl: profile.linkedInUrl ?? '',
      photoDocumentId: profile.photoDocumentId ?? null,
      de: toTextValue(profile.de),
      en: toTextValue(profile.en),
      facts: (profile.facts ?? []).map((fact) => ({
        value: fact.value ?? '',
        labelDe: fact.labelDe ?? '',
        labelEn: fact.labelEn ?? '',
      })),
    });
  }

  private toProfile(): AdminProfile {
    const value = this.form.getRawValue();
    return {
      fullName: value.fullName,
      email: value.email,
      phone: value.phone,
      location: value.location,
      addressLines: textToLines(value.addressLines),
      gitHubUrl: value.gitHubUrl,
      linkedInUrl: value.linkedInUrl,
      photoDocumentId: value.photoDocumentId ?? undefined,
      de: value.de,
      en: value.en,
      facts: value.facts satisfies FactForm[],
    };
  }

  private textGroup() {
    return this.fb.group({ titleLine: '', headline: '', intro: '', availabilityLine: '' });
  }
}

function createFactGroup(fb: NonNullableFormBuilder) {
  return fb.group({ value: '', labelDe: '', labelEn: '' });
}

function toTextValue(texts: ProfileTextsForm | undefined): Required<Record<TextKey, string>> {
  return {
    titleLine: texts?.titleLine ?? '',
    headline: texts?.headline ?? '',
    intro: texts?.intro ?? '',
    availabilityLine: texts?.availabilityLine ?? '',
  };
}

function isPublicImage(document: AdminDocument): boolean {
  return document.visibility === 'Public' && (document.contentType ?? '').startsWith('image/');
}
