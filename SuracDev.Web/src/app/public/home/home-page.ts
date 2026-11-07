import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  PublicEducation,
  PublicExperience,
  PublicProfile,
  PublicProjectCard,
} from '../../api/generated/public.client';
import { formatPeriod } from '../../core/format/period';
import { UiStrings, uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { Seo } from '../../core/seo/seo';
import { SOURCE_REPOSITORY_URL } from '../../layout/site-footer/site-footer';
import { Fact } from '../../ui/fact-bar/fact';
import { UiFactBar } from '../../ui/fact-bar/fact-bar';
import { HeroLink, UiHero } from '../../ui/hero/hero';
import { ProjectCardLink, UiProjectCard } from '../../ui/project-card/project-card';
import { UiSectionHeading } from '../../ui/section-heading/section-heading';
import { SkillColumn, UiSkillColumns } from '../../ui/skill-columns/skill-columns';
import { TimelineEntry, UiTimeline } from '../../ui/timeline/timeline';
import { valueOf } from '../content/load-state';
import { documentUrl, PublicContent } from '../content/public-content';

const DESCRIPTION_LENGTH = 160;

/** A featured project prepared for its card. */
interface ProjectCardView {
  readonly id: number;
  readonly title: string;
  readonly teaser: string;
  readonly status: string;
  readonly period: string | null;
  readonly stackTags: readonly string[];
  readonly caseStudyPath: string;
  readonly links: readonly ProjectCardLink[];
}

/**
 * The start page in the public section order (Shared/Application/Audience in the API): hero, fact
 * bar, profile, career, featured projects, skills, working mode, contact. HR-first: name, role,
 * location and contact are above the fold (docs/decisions.md).
 */
@Component({
  selector: 'app-home-page',
  imports: [UiFactBar, UiHero, UiProjectCard, UiSectionHeading, UiSkillColumns, UiTimeline],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly content = inject(PublicContent);
  private readonly seo = inject(Seo);

  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');

  protected readonly strings = computed(() => uiStrings(this.locale()));
  protected readonly repositoryUrl = SOURCE_REPOSITORY_URL;

  private readonly profileResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.profile(params),
  });
  private readonly experiencesResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.experiences(params),
  });
  private readonly educationResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.education(params),
  });
  private readonly projectsResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.featuredProjects(params),
  });
  private readonly skillsResource = rxResource({
    params: () => this.locale(),
    stream: ({ params }) => this.content.skillGroups(params),
  });

  protected readonly profile = computed(() => valueOf(this.profileResource));
  protected readonly profileLinks = computed(() => toProfileLinks(this.profile()));
  protected readonly hero = computed(() => {
    const profile = this.profile();
    if (!profile) {
      return null;
    }

    return {
      name: profile.fullName ?? '',
      titleLine: profile.titleLine ?? '',
      headline: profile.headline ?? '',
      location: profile.location ?? '',
      email: profile.email ?? '',
      photoUrl: profile.photoDocumentId ? documentUrl(profile.photoDocumentId) : null,
      photoAlt: this.strings().hero.photoAlt(profile.fullName ?? ''),
    };
  });
  protected readonly intro = computed(() => this.profile()?.intro ?? null);
  protected readonly availability = computed(() => this.profile()?.availabilityLine ?? null);
  protected readonly facts = computed<Fact[]>(() =>
    (this.profile()?.facts ?? []).map((fact) => ({ value: fact.value ?? '', label: fact.label ?? '' })),
  );
  protected readonly experiences = computed(() =>
    (valueOf(this.experiencesResource) ?? []).map((item) => toExperienceEntry(item, this.strings())),
  );
  protected readonly education = computed(() =>
    (valueOf(this.educationResource) ?? []).map((item) => toEducationEntry(item, this.strings())),
  );
  protected readonly projects = computed(() =>
    (valueOf(this.projectsResource) ?? []).map((item) => toProjectCard(item, this.strings(), this.locale())),
  );
  protected readonly skillGroups = computed<SkillColumn[]>(() =>
    (valueOf(this.skillsResource) ?? []).map((group) => ({
      id: group.id ?? 0,
      name: group.name ?? '',
      skills: (group.skills ?? []).map((skill) => ({
        name: skill.name ?? '',
        sinceYear: skill.sinceYear,
        qualifier: skill.qualifier,
      })),
    })),
  );
  protected readonly hasCareer = computed(() => this.experiences().length + this.education().length > 0);

  /** Numbers follow the sections actually shown, so a hidden section leaves no gap. */
  protected readonly numbers = computed(() => {
    const shown = ['profile', 'career', 'projects', 'skills', 'workingMode', 'contact'].filter((key) =>
      this.isShown(key),
    );
    return Object.fromEntries(shown.map((key, index) => [key, String(index + 1).padStart(2, '0')]));
  });

  constructor() {
    effect(() => {
      const profile = this.profile();
      if (profile) {
        this.seo.apply({
          title: `${profile.fullName} – ${profile.titleLine}`,
          description: (profile.intro ?? profile.headline ?? '').slice(0, DESCRIPTION_LENGTH),
          locale: this.locale(),
          paths: { de: '/', en: '/en' },
        });
      }
    });
  }

  private isShown(key: string): boolean {
    switch (key) {
      case 'profile':
        return !!this.profile()?.intro;
      case 'career':
        return this.hasCareer();
      case 'projects':
        return this.projects().length > 0;
      case 'skills':
        return this.skillGroups().length > 0;
      default:
        return true;
    }
  }
}

function toProfileLinks(profile: PublicProfile | undefined): HeroLink[] {
  const links: HeroLink[] = [];
  if (profile?.gitHubUrl) {
    links.push({ label: 'GitHub', href: profile.gitHubUrl });
  }
  if (profile?.linkedInUrl) {
    links.push({ label: 'LinkedIn', href: profile.linkedInUrl });
  }
  return links;
}

function toExperienceEntry(item: PublicExperience, strings: UiStrings): TimelineEntry {
  return {
    id: item.id ?? 0,
    period: formatPeriod(item.startDate, item.endDate, strings.career.openEnd),
    title: item.roleTitle ?? '',
    organisation: item.company ?? '',
    location: item.location,
    summary: item.summary,
    bullets: item.bullets ?? [],
    detail: item.stackLine,
  };
}

function toEducationEntry(item: PublicEducation, strings: UiStrings): TimelineEntry {
  return {
    id: item.id ?? 0,
    period: formatPeriod(item.startDate, item.endDate, strings.career.openEnd),
    title: item.degree ?? '',
    organisation: item.institution ?? '',
    location: item.location,
    summary: item.summary,
    bullets: [],
    detail: item.grade ? `${strings.career.grade} ${item.grade}` : null,
  };
}

function toProjectCard(item: PublicProjectCard, strings: UiStrings, locale: Locale): ProjectCardView {
  const links: ProjectCardLink[] = [];
  if (item.liveUrl) {
    links.push({ label: strings.projects.live, href: item.liveUrl });
  }
  if (item.repositoryUrl) {
    links.push({ label: strings.projects.source, href: item.repositoryUrl });
  }
  return {
    id: item.id ?? 0,
    title: item.title ?? '',
    teaser: item.teaser ?? '',
    status: strings.projects.statuses[item.status ?? ''] ?? '',
    period: projectPeriod(item, strings),
    stackTags: item.stackTags ?? [],
    caseStudyPath: locale === 'de' ? `/projekte/${item.slug}` : `/en/projects/${item.slug}`,
    links,
  };
}

function projectPeriod(item: PublicProjectCard, strings: UiStrings): string | null {
  if (!item.startedYear) {
    return null;
  }
  return item.endedYear
    ? `${item.startedYear} – ${item.endedYear}`
    : `${strings.projects.since} ${item.startedYear}`;
}
