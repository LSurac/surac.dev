using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Legal.Domain;

/// <summary>Which legal page. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum LegalPageKey
{
  Unknown = 0,
  Imprint = 1,
  Privacy = 2,
}

/// <summary>The texts of a legal page in one locale.</summary>
public sealed record LegalTexts(string? Title, string? BodyMarkdown)
{
  /// <summary>True when neither field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(BodyMarkdown);
}

/// <summary>
/// Impressum or privacy page as Markdown. Each locale carries its own "last updated" stamp, because
/// the privacy page shows it and the two languages are not always edited together.
/// </summary>
public sealed class LegalPage
{
  private readonly List<LegalPageTranslation> translations = [];

  private LegalPage()
  {
  }

  public long Id { get; private set; }

  public LegalPageKey Key { get; private set; }

  public IReadOnlyList<LegalPageTranslation> Translations => translations;

  /// <summary>Creates a page for <paramref name="key"/> with the mandatory German texts.</summary>
  public static LegalPage Create(LegalPageKey key, LegalTexts german, DateTimeOffset now)
  {
    var page = new LegalPage { Key = Require.Known(key, "key") };
    page.SetTexts(Locale.German, german, now);
    return page;
  }

  /// <summary>Writes one locale; only an actual change moves that locale's "last updated" stamp.</summary>
  public void SetTexts(Locale locale, LegalTexts texts, DateTimeOffset now)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => LegalPageTranslation.Create(locale, texts, now),
      translation => translation.Apply(texts, now));
}

/// <summary>The texts of a legal page in one locale with the time they last changed.</summary>
public sealed class LegalPageTranslation : ITranslation
{
  private LegalPageTranslation()
  {
  }

  public long Id { get; private set; }

  public long LegalPageId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Title { get; private set; }

  public string? BodyMarkdown { get; private set; }

  public DateTimeOffset UpdatedAt { get; private set; }

  internal static LegalPageTranslation Create(Locale locale, LegalTexts texts, DateTimeOffset now)
  {
    var translation = new LegalPageTranslation { Locale = locale };
    translation.Apply(texts, now);
    return translation;
  }

  internal void Apply(LegalTexts texts, DateTimeOffset now)
  {
    var title = Require.Translated(texts.Title, Locale, "title", TextLimits.Name, required: true);
    var body = Require.Translated(texts.BodyMarkdown, Locale, "bodyMarkdown", TextLimits.Markdown, required: true);
    if (title == Title && body == BodyMarkdown)
    {
      return;
    }

    Title = title;
    BodyMarkdown = body;
    UpdatedAt = now;
  }
}
