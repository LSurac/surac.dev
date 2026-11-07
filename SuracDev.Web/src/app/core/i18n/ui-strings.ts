import { Locale } from '../locale/locale';

/**
 * Chrome strings of the site (navigation, buttons, labels, the working-mode section). Content
 * strings come from the API. Both locales are complete by construction: the type forces every key
 * in every language.
 */
export interface UiStrings {
  readonly skipToContent: string;
  readonly siteName: string;
  readonly nav: {
    readonly projects: string;
    readonly career: string;
    readonly workingMode: string;
    readonly about: string;
  };
  readonly localeSwitch: { readonly label: string; readonly target: string };
  readonly footer: {
    readonly imprint: string;
    readonly privacy: string;
    readonly sourceCode: string;
    readonly lastUpdated: string;
  };
  readonly sections: {
    readonly profile: string;
    readonly career: string;
    readonly experiences: string;
    readonly education: string;
    readonly projects: string;
    readonly skills: string;
    readonly workingMode: string;
    readonly contact: string;
  };
  readonly hero: {
    readonly email: string;
    readonly photoAlt: (name: string) => string;
  };
  readonly career: { readonly openEnd: string; readonly grade: string };
  readonly projects: {
    readonly caseStudy: string;
    readonly live: string;
    readonly source: string;
    readonly since: string;
    readonly statuses: Readonly<Record<string, string>>;
  };
  readonly skills: { readonly since: string };
  readonly workingMode: {
    readonly paragraphs: readonly string[];
    readonly repository: string;
  };
  readonly contact: { readonly lede: string; readonly email: string };
  readonly caseStudy: {
    readonly back: string;
    readonly problem: string;
    readonly role: string;
    readonly decisions: string;
    readonly outcome: string;
    readonly caseStudy: string;
  };
  readonly legal: { readonly updated: string; readonly provider: string };
  readonly notFound: { readonly title: string; readonly text: string; readonly home: string };
  readonly unavailable: { readonly title: string; readonly text: string };
}

const de: UiStrings = {
  skipToContent: 'Zum Inhalt springen',
  siteName: 'Leonard Surac',
  nav: { projects: 'Projekte', career: 'Werdegang', workingMode: 'Arbeitsweise', about: 'Über mich' },
  localeSwitch: { label: 'Sprache', target: 'English' },
  footer: {
    imprint: 'Impressum',
    privacy: 'Datenschutz',
    sourceCode: 'Quellcode dieser Seite',
    lastUpdated: 'Stand',
  },
  sections: {
    profile: 'Profil',
    career: 'Werdegang',
    experiences: 'Berufserfahrung',
    education: 'Ausbildung',
    projects: 'Projekte',
    skills: 'Kenntnisse',
    workingMode: 'Arbeitsweise',
    contact: 'Kontakt',
  },
  hero: { email: 'E-Mail schreiben', photoAlt: (name) => `Porträt von ${name}` },
  career: { openEnd: 'heute', grade: 'Note' },
  projects: {
    caseStudy: 'Fallstudie lesen',
    live: 'Live ansehen',
    source: 'Quellcode',
    since: 'seit',
    statuses: { Live: 'Live', InProduction: 'Im Betrieb', Archived: 'Archiviert', Planned: 'Geplant' },
  },
  skills: { since: 'seit' },
  workingMode: {
    paragraphs: [
      'Ich entwickle mit KI-Agenten als Werkzeug. Jede Aufgabe beginnt mit einem Plan, den ich abnehme, ' +
        'und mit festen Regeln im Repository: Architektur, Teststrategie und Code-Stil stehen in AGENTS.md ' +
        'und den Dokumenten unter docs/.',
      'Agenten suchen im Code, schreiben Tests und Implementierung nach diesen Regeln und führen die ' +
        'Prüfungen aus. Architektur, Datenmodell, Sicherheit und die Abnahme jeder Änderung bleiben bei mir.',
      'Diese Seite ist so entstanden. Ihr Quellcode zeigt, wie das im Alltag aussieht.',
    ],
    repository: 'Quellcode dieser Seite auf GitHub',
  },
  contact: { lede: 'Am schnellsten erreichen Sie mich per E-Mail.', email: 'E-Mail' },
  caseStudy: {
    back: 'Zur Startseite',
    problem: 'Problem',
    role: 'Meine Rolle',
    decisions: 'Entscheidungen',
    outcome: 'Ergebnis',
    caseStudy: 'Fallstudie',
  },
  legal: { updated: 'Stand', provider: 'Anbieter' },
  notFound: {
    title: 'Seite nicht gefunden',
    text: 'Diese Seite gibt es nicht oder nicht mehr.',
    home: 'Zur Startseite',
  },
  unavailable: {
    title: 'Gerade nicht erreichbar',
    text: 'Der Inhalt konnte nicht geladen werden. Bitte später erneut versuchen.',
  },
};

const en: UiStrings = {
  skipToContent: 'Skip to content',
  siteName: 'Leonard Surac',
  nav: { projects: 'Projects', career: 'Career', workingMode: 'How I work', about: 'About' },
  localeSwitch: { label: 'Language', target: 'Deutsch' },
  footer: {
    imprint: 'Legal notice',
    privacy: 'Privacy',
    sourceCode: 'Source code of this site',
    lastUpdated: 'Updated',
  },
  sections: {
    profile: 'Profile',
    career: 'Career',
    experiences: 'Experience',
    education: 'Education',
    projects: 'Projects',
    skills: 'Skills',
    workingMode: 'How I work',
    contact: 'Contact',
  },
  hero: { email: 'Write an e-mail', photoAlt: (name) => `Portrait of ${name}` },
  career: { openEnd: 'present', grade: 'Grade' },
  projects: {
    caseStudy: 'Read the case study',
    live: 'See it live',
    source: 'Source code',
    since: 'since',
    statuses: { Live: 'Live', InProduction: 'In production', Archived: 'Archived', Planned: 'Planned' },
  },
  skills: { since: 'since' },
  workingMode: {
    paragraphs: [
      'I develop with AI agents as a tool. Every task starts with a plan I approve and with fixed rules ' +
        'in the repository: architecture, test strategy and code style are written down in AGENTS.md and ' +
        'the documents under docs/.',
      'Agents search the code, write tests and implementation by those rules and run the checks. ' +
        'Architecture, data model, security and the acceptance of every change stay with me.',
      'This site was built that way. Its source code shows what that looks like day to day.',
    ],
    repository: 'Source code of this site on GitHub',
  },
  contact: { lede: 'E-mail is the fastest way to reach me.', email: 'E-mail' },
  caseStudy: {
    back: 'Back to the start page',
    problem: 'Problem',
    role: 'My role',
    decisions: 'Decisions',
    outcome: 'Outcome',
    caseStudy: 'Case study',
  },
  legal: { updated: 'Updated', provider: 'Provider' },
  notFound: {
    title: 'Page not found',
    text: 'This page does not exist or no longer exists.',
    home: 'Back to the start page',
  },
  unavailable: {
    title: 'Temporarily unavailable',
    text: 'The content could not be loaded. Please try again later.',
  },
};

const strings: Record<Locale, UiStrings> = { de, en };

export function uiStrings(locale: Locale): UiStrings {
  return strings[locale];
}
