namespace SuracDev.Api.Shared.Application.Localization;

/// <summary>
/// Writes one locale into an entity's translation list. German always exists (its required fields
/// are validated by the translation itself); an English set without any text is removed, so the
/// reader falls back to German instead of showing blanks.
/// </summary>
public static class TranslationList
{
  /// <summary>Creates, updates or (for an empty English set) removes the translation of
  /// <paramref name="locale"/>.</summary>
  public static void Set<TTranslation>(
    List<TTranslation> translations,
    Locale locale,
    bool isEmpty,
    Func<TTranslation> create,
    Action<TTranslation> update)
    where TTranslation : class, ITranslation
  {
    var existing = translations.SingleOrDefault(translation => translation.Locale == locale);

    if (isEmpty && !locale.IsPrimary)
    {
      if (existing is not null)
      {
        translations.Remove(existing);
      }

      return;
    }

    if (existing is null)
    {
      translations.Add(create());
      return;
    }

    update(existing);
  }
}
