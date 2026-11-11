import { Locale } from '../locale/locale';
import { SITE_ORIGIN } from './seo';

/** A schema.org JSON-LD document, written into the page head by Seo. */
export type StructuredData = Readonly<Record<string, unknown>>;

/** What the start page knows about the person behind the site; `imagePath` is a public site path. */
export interface PersonFacts {
  readonly fullName?: string;
  readonly titleLine?: string;
  readonly location?: string;
  readonly gitHubUrl?: string;
  readonly linkedInUrl?: string;
  readonly imagePath?: string | null;
}

/** A case study as a work of the person; `path` is the page's path in the shown locale. */
export interface CaseStudyFacts {
  readonly title?: string;
  readonly teaser?: string;
  readonly stackTags?: readonly string[];
  readonly path: string;
  readonly imagePath?: string | null;
}

const CONTEXT = 'https://schema.org';
const HOME = `${SITE_ORIGIN}/`;
const PERSON_ID = `${SITE_ORIGIN}/#person`;

/**
 * The start page: the person (with GitHub and LinkedIn as `sameAs`, so search engines connect the
 * profiles to the name) and the site, published by that person. Empty facts are left out.
 */
export function homeData(person: PersonFacts, skills: readonly string[], locale: Locale) {
  const sameAs = [person.gitHubUrl, person.linkedInUrl].filter((url): url is string => !!url);
  return {
    '@context': CONTEXT,
    '@graph': [
      withoutEmpty({
        '@type': 'Person',
        '@id': PERSON_ID,
        name: person.fullName,
        jobTitle: person.titleLine,
        url: HOME,
        image: person.imagePath ? SITE_ORIGIN + person.imagePath : undefined,
        homeLocation: person.location ? { '@type': 'Place', name: person.location } : undefined,
        sameAs: sameAs.length > 0 ? sameAs : undefined,
        knowsAbout: skills.length > 0 ? skills : undefined,
      }),
      {
        '@type': 'WebSite',
        '@id': `${SITE_ORIGIN}/#website`,
        url: HOME,
        name: 'surac.dev',
        inLanguage: locale,
        publisher: { '@id': PERSON_ID },
      },
    ],
  };
}

/** "Über mich" / "About me" is the profile page of the person the start page describes. */
export function profilePageData(name: string, title: string, path: string, locale: Locale): StructuredData {
  return {
    '@context': CONTEXT,
    '@type': 'ProfilePage',
    url: SITE_ORIGIN + path,
    name: title,
    inLanguage: locale,
    mainEntity: personReference(name),
  };
}

export function caseStudyData(study: CaseStudyFacts, author: string, locale: Locale): StructuredData {
  return withoutEmpty({
    '@context': CONTEXT,
    '@type': 'CreativeWork',
    name: study.title,
    description: study.teaser,
    url: SITE_ORIGIN + study.path,
    inLanguage: locale,
    keywords: study.stackTags?.length ? study.stackTags.join(', ') : undefined,
    image: study.imagePath ? SITE_ORIGIN + study.imagePath : undefined,
    author: personReference(author),
  });
}

function personReference(name: string) {
  return { '@type': 'Person', '@id': PERSON_ID, name, url: HOME };
}

function withoutEmpty(node: Record<string, unknown>): StructuredData {
  return Object.fromEntries(Object.entries(node).filter(([, value]) => value !== undefined && value !== ''));
}
