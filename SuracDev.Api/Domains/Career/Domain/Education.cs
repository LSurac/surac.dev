using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Career.Domain;

/// <summary>The structural data of an education entry as the admin enters it.</summary>
public sealed record EducationDetails(
  string? Institution,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  string? Grade,
  int SortOrder,
  bool IsVisible);

/// <summary>The translatable texts of an education entry in one locale.</summary>
public sealed record EducationTexts(string? Degree, string? Summary)
{
  /// <summary>True when no field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(Degree) && string.IsNullOrWhiteSpace(Summary);
}

/// <summary>School, apprenticeship or studies. The grade is free text ("1,8", "gut").</summary>
public sealed class Education
{
  public const int MaxGradeLength = 40;

  private readonly List<EducationTranslation> translations = [];

  private Education()
  {
  }

  public long Id { get; private set; }

  public string Institution { get; private set; } = null!;

  public string? Location { get; private set; }

  public DateOnly StartDate { get; private set; }

  public DateOnly? EndDate { get; private set; }

  public string? Grade { get; private set; }

  public int SortOrder { get; private set; }

  public bool IsVisible { get; private set; }

  public IReadOnlyList<EducationTranslation> Translations => translations;

  /// <summary>Creates an entry from its details and the mandatory German texts.</summary>
  public static Education Create(EducationDetails details, EducationTexts german)
  {
    var education = new Education();
    education.UpdateDetails(details);
    education.SetTexts(Locale.German, german);
    return education;
  }

  /// <summary>Replaces the structural data after validating it as a whole.</summary>
  public void UpdateDetails(EducationDetails details)
  {
    Require.DateRange(details.StartDate, details.EndDate);
    Institution = Require.Text(details.Institution, "institution", TextLimits.Name);
    Location = Require.OptionalText(details.Location, "location", TextLimits.Name);
    StartDate = details.StartDate;
    EndDate = details.EndDate;
    Grade = Require.OptionalText(details.Grade, "grade", MaxGradeLength);
    SortOrder = details.SortOrder;
    IsVisible = details.IsVisible;
  }

  /// <summary>Writes the texts of one locale; clearing every English field removes the English set.</summary>
  public void SetTexts(Locale locale, EducationTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => EducationTranslation.Create(locale, texts),
      translation => translation.Apply(texts));
}

/// <summary>The texts of an education entry in one locale.</summary>
public sealed class EducationTranslation : ITranslation
{
  private EducationTranslation()
  {
  }

  public long Id { get; private set; }

  public long EducationId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Degree { get; private set; }

  public string? Summary { get; private set; }

  internal static EducationTranslation Create(Locale locale, EducationTexts texts)
  {
    var translation = new EducationTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(EducationTexts texts)
  {
    Degree = Require.Translated(texts.Degree, Locale, "degree", TextLimits.Name, required: true);
    Summary = Require.Translated(texts.Summary, Locale, "summary", TextLimits.Paragraph);
  }
}
