import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  LegalPageKey,
  PublicCareerClient,
  PublicCaseStudy,
  PublicEducation,
  PublicExperience,
  PublicLegalClient,
  PublicLegalPage,
  PublicProfile,
  PublicProfileClient,
  PublicProjectCard,
  PublicProjectsClient,
  PublicSkillGroup,
  PublicSkillsClient,
} from '../../api/generated/public.client';
import { Locale } from '../../core/locale/locale';

/** Public URL of an uploaded public document (photo, case-study image). */
export function documentUrl(id: number): string {
  return `/api/public/documents/${id}`;
}

/**
 * The content of the open site, one locale per call. A thin layer over the generated public
 * clients so pages ask for content in the site's terms and never build URLs themselves. Requests
 * are plain HttpClient GETs, so the server render's responses are replayed during hydration.
 */
@Injectable({ providedIn: 'root' })
export class PublicContent {
  private readonly profiles = inject(PublicProfileClient);
  private readonly career = inject(PublicCareerClient);
  private readonly projects = inject(PublicProjectsClient);
  private readonly skills = inject(PublicSkillsClient);
  private readonly legal = inject(PublicLegalClient);

  profile(locale: Locale): Observable<PublicProfile> {
    return this.profiles.get(locale);
  }

  experiences(locale: Locale): Observable<PublicExperience[]> {
    return this.career.listExperiences(locale);
  }

  education(locale: Locale): Observable<PublicEducation[]> {
    return this.career.listEducation(locale);
  }

  featuredProjects(locale: Locale): Observable<PublicProjectCard[]> {
    return this.projects.list(locale, true);
  }

  caseStudy(slug: string, locale: Locale): Observable<PublicCaseStudy> {
    return this.projects.get(slug, locale);
  }

  skillGroups(locale: Locale): Observable<PublicSkillGroup[]> {
    return this.skills.list(locale);
  }

  legalPage(key: LegalPageKey, locale: Locale): Observable<PublicLegalPage> {
    return this.legal.get(key, locale);
  }
}
