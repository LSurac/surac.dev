/**
 * Texts of the admin area. The admin has exactly one, German-speaking user, so these exist in
 * German only (docs/decisions.md); the public site's chrome strings live in core/i18n with both
 * locales. Kept out of the templates so wording changes in one place.
 */
export const adminStrings = {
  title: 'Verwaltung',
  locale: { german: 'Deutsch', english: 'English', untranslated: 'nicht übersetzt' },
  login: {
    heading: 'Anmelden',
    firstHeading: 'Passwort festlegen',
    firstHint: 'Der erste Login legt das Passwort fest: mindestens 12 Zeichen.',
    password: 'Passwort',
    submit: 'Anmelden',
    firstSubmit: 'Passwort festlegen',
    failed: 'Anmeldung fehlgeschlagen',
    tooMany: 'Zu viele Versuche. Bitte in einer Viertelstunde erneut versuchen.',
  },
  nav: {
    profile: 'Profil',
    experiences: 'Werdegang',
    education: 'Ausbildung',
    projects: 'Projekte',
    skills: 'Kenntnisse',
    legal: 'Rechtstexte',
    documents: 'Dokumente',
    site: 'Zur Website',
    logout: 'Abmelden',
  },
  form: {
    save: 'Speichern',
    saved: 'Gespeichert.',
    delete: 'Löschen',
    confirmDelete: 'Diesen Eintrag wirklich löschen?',
    saveFailed: 'Nicht gespeichert',
    loadFailed: 'Konnte nicht geladen werden.',
    unexpected: 'Unerwarteter Fehler. Bitte erneut versuchen.',
    add: 'Hinzufügen',
    remove: 'Entfernen',
    moveUp: 'Nach oben',
    moveDown: 'Nach unten',
    visible: 'Auf der Website sichtbar',
    sortOrder: 'Reihenfolge',
    linesHint: 'Eine Zeile pro Eintrag.',
    none: '–',
  },
} as const;
