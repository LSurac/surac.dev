import { Locale } from '../locale/locale';

/**
 * Chrome strings of the site (navigation, buttons, labels). Content strings come from the API.
 * Both locales are complete by construction: the type forces every key in every language.
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
  readonly foundation: {
    readonly kicker: string;
    readonly headline: string;
    readonly lede: string;
  };
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
  foundation: {
    kicker: 'Fullstack-Entwickler · .NET & Angular',
    headline: 'Diese Seite entsteht gerade.',
    lede: 'Der Quellcode ist von der ersten Zeile an öffentlich. Inhalte folgen, sobald das Fundament steht.',
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
  foundation: {
    kicker: 'Full-stack developer · .NET & Angular',
    headline: 'This site is being built.',
    lede: 'The source code has been public from the first line. Content follows once the foundation stands.',
  },
};

const strings: Record<Locale, UiStrings> = { de, en };

export function uiStrings(locale: Locale): UiStrings {
  return strings[locale];
}
