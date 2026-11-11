import { ViewportScroller } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  Injector,
  input,
} from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import {
  PublicEducation,
  PublicExperience,
  PublicProfile,
  PublicProjectCard,
  SiteSection,
} from '../../api/generated/public.client';
import { formatPeriod } from '../../core/format/period';
import { UiStrings, uiStrings } from '../../core/i18n/ui-strings';
import { Locale } from '../../core/locale/locale';
import { Seo } from '../../core/seo/seo';
import { homeData } from '../../core/seo/structured-data';
import { SOURCE_REPOSITORY_URL } from '../../layout/site-footer/site-footer';
import { Fact } from '../../ui/fact-bar/fact';
import { UiFactBar } from '../../ui/fact-bar/fact-bar';
import { HeroDownload, HeroLink, UiHero } from '../../ui/hero/hero';
import { ProjectCardLink, UiProjectCard } from '../../ui/project-card/project-card';
import { DownloadItem, UiDownloadList } from '../../ui/download-list/download-list';
import { UiSectionHeading } from '../../ui/section-heading/section-heading';
import { SkillColumn, UiSkillColumns } from '../../ui/skill-columns/skill-columns';
import { TimelineEntry, UiTimeline } from '../../ui/timeline/timeline';
import { valueOf } from '../content/load-state';
import { documentUrl, PublicContent } from '../content/public-content';

const DESCRIPTION_LENGTH = 160;

/**
 * The section order of the open site, mirrored from `SectionOrder` in the API
 * (Shared/Application/Audience). Every profile opens with hero and fact bar and closes with
 * contact; only the sections in between move.
 */
export const PUBLIC_SECTIONS: readonly SiteSection[] = [
  'Hero',
  'Facts',
  'Intro',
  'Career',
  'Projects',
  'Skills',
  'WorkingMode',
  'Contact',
];

/** The sections between the fact bar and the contact section, the ones a profile reorders. */
const BODY_SECTIONS: readonly SiteSection[] = ['Intro', 'Career', 'Projects', 'Skills', 'WorkingMode'];

/**
 * What an application link changes on the start page (docs/decisions.md, "One site, three
 * audience profiles"): the section order, a greeting and the downloads. Never the content.
 */
export interface LinkAudience {
  readonly sections: readonly SiteSection[];
  readonly greeting: string | null;
  readonly cvHref: string;
  readonly documents: readonly DownloadItem[];
}

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
 * The start page: hero, fact bar, then profile, career, featured projects, skills and working mode
 * in the order of the audience, then contact. HR-first: name, role, location and contact are above
 * the fold (docs/decisions.md). Without an audience it is the open site in the public order; an
 * application link passes its own order, greeting and downloads.
 */
@Component({
  selector: 'app-home-page',
  imports: [UiDownloadList, UiFactBar, UiHero, UiProjectCard, UiSectionHeading, UiSkillColumns, UiTimeline],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly content = inject(PublicContent);
  private readonly seo = inject(Seo);
  private readonly scroller = inject(ViewportScroller);
  private readonly injector = inject(Injector);
  private readonly fragment = toSignal(inject(ActivatedRoute).fragment, { initialValue: null });

  /** Bound from the route data by withComponentInputBinding. */
  readonly locale = input<Locale>('de');
  /** Set by the application-link page; null on the open site. */
  readonly audience = input<LinkAudience | null>(null);

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
  /** The CV in the page language, the link's own when there is one; the server names the file. */
  protected readonly cvDownload = computed<HeroDownload>(() => ({
    label: this.strings().hero.cv,
    href: this.audience()?.cvHref ?? `/api/public/cv.pdf?locale=${this.locale()}`,
    fileName: '',
  }));
  protected readonly greeting = computed(() => this.audience()?.greeting ?? null);
  protected readonly documents = computed(() => this.audience()?.documents ?? []);
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
  /** Short notes under the intro; empty when there is none, so no empty paragraph is rendered. */
  protected readonly profileNotes = computed(() =>
    [this.profile()?.availabilityLine].filter((line): line is string => !!line),
  );
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

  /**
   * The reorderable sections in the audience's order. The list depends on the order only, never on
   * loaded content: the browser's first render must produce the same list as the server render, or
   * hydration pairs the server's section markup with the wrong entries. Empty sections hide inside
   * their own block instead.
   */
  protected readonly bodySections = computed(() =>
    (this.audience()?.sections ?? PUBLIC_SECTIONS).filter((section) => BODY_SECTIONS.includes(section)),
  );

  /** Numbers follow the sections actually shown, so a hidden section leaves no gap. */
  protected readonly numbers = computed(() => {
    const shown: SiteSection[] = [
      ...this.bodySections().filter((section) => this.isShown(section)),
      'Contact',
    ];
    return Object.fromEntries(shown.map((key, index) => [key, String(index + 1).padStart(2, '0')]));
  });

  /** True once every section's content has arrived (or failed), so every anchor exists. */
  private readonly contentSettled = computed(
    () =>
      ![
        this.profileResource,
        this.experiencesResource,
        this.educationResource,
        this.projectsResource,
        this.skillsResource,
      ].some((resource) => resource.isLoading()),
  );

  constructor() {
    // The router scrolls to a fragment right after navigating, before the sections exist when the
    // visitor comes from another page ("Über mich" → "Projekte"). Scroll again once they rendered.
    effect(() => {
      const fragment = this.fragment();
      if (!fragment || !this.contentSettled()) {
        return;
      }

      afterNextRender(() => this.scroller.scrollToAnchor(fragment), { injector: this.injector });
    });
    effect(() => {
      const profile = this.profile();
      if (profile) {
        const imagePath = profile.photoDocumentId ? documentUrl(profile.photoDocumentId) : null;
        const skills = (valueOf(this.skillsResource) ?? []).flatMap((group) =>
          (group.skills ?? []).map((skill) => skill.name ?? ''),
        );
        this.seo.apply({
          title: `${profile.fullName} – ${profile.titleLine}`,
          description: (profile.intro ?? profile.headline ?? '').slice(0, DESCRIPTION_LENGTH),
          locale: this.locale(),
          paths: { de: '/', en: '/en' },
          imagePath,
          structuredData: homeData({ ...profile, imagePath }, skills.filter(Boolean), this.locale()),
        });
      }
    });
  }

  private isShown(section: SiteSection): boolean {
    switch (section) {
      case 'Intro':
        return !!this.intro();
      case 'Career':
        return this.hasCareer();
      case 'Projects':
        return this.projects().length > 0;
      case 'Skills':
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
