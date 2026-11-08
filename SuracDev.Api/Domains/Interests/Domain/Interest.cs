using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Interests.Domain;

/// <summary>The topic of an interest. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum InterestKind
{
  Unknown = 0,
  Music = 1,
  Chess = 2,
  Homelab = 3,
  Family = 4,
  Other = 5,
}

/// <summary>The structural data of an interest as the admin enters it.</summary>
public sealed record InterestDetails(InterestKind Kind, int SortOrder, bool IsVisible, long? ImageDocumentId);

/// <summary>The translatable texts of an interest in one locale.</summary>
public sealed record InterestTexts(string? Title, string? Body)
{
  /// <summary>True when neither field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Body);
}

/// <summary>One section of the "Über mich" page. Never shown on the start page.</summary>
public sealed class Interest
{
  private readonly List<InterestTranslation> translations = [];

  private Interest()
  {
  }

  public long Id { get; private set; }

  public InterestKind Kind { get; private set; }

  public int SortOrder { get; private set; }

  public bool IsVisible { get; private set; }

  public long? ImageDocumentId { get; private set; }

  public IReadOnlyList<InterestTranslation> Translations => translations;

  /// <summary>Creates an interest from its details and the mandatory German texts.</summary>
  public static Interest Create(InterestDetails details, InterestTexts german)
  {
    var interest = new Interest();
    interest.UpdateDetails(details);
    interest.SetTexts(Locale.German, german);
    return interest;
  }

  /// <summary>Replaces the structural data after validating it.</summary>
  public void UpdateDetails(InterestDetails details)
  {
    Kind = Require.Known(details.Kind, "kind");
    SortOrder = details.SortOrder;
    IsVisible = details.IsVisible;
    ImageDocumentId = details.ImageDocumentId;
  }

  /// <summary>Writes the texts of one locale; clearing every English field removes the English set.</summary>
  public void SetTexts(Locale locale, InterestTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => InterestTranslation.Create(locale, texts),
      translation => translation.Apply(texts));
}

/// <summary>The texts of an interest in one locale; the body is Markdown.</summary>
public sealed class InterestTranslation : ITranslation
{
  private InterestTranslation()
  {
  }

  public long Id { get; private set; }

  public long InterestId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Title { get; private set; }

  public string? Body { get; private set; }

  internal static InterestTranslation Create(Locale locale, InterestTexts texts)
  {
    var translation = new InterestTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(InterestTexts texts)
  {
    Title = Require.Translated(texts.Title, Locale, "title", TextLimits.Name, required: true);
    Body = Require.Translated(texts.Body, Locale, "body", TextLimits.Markdown);
  }
}
