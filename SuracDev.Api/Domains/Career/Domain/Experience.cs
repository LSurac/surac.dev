using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Career.Domain;

/// <summary>The structural data of a career station as the admin enters it.</summary>
public sealed record ExperienceDetails(
  string? Company,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  int SortOrder,
  bool IsVisible);

/// <summary>The translatable texts of a career station in one locale.</summary>
public sealed record ExperienceTexts(
  string? RoleTitle,
  string? Summary,
  IReadOnlyList<string?>? Bullets,
  string? StackLine)
{
  /// <summary>True when no field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(RoleTitle) && string.IsNullOrWhiteSpace(Summary)
    && (Bullets ?? []).All(string.IsNullOrWhiteSpace) && string.IsNullOrWhiteSpace(StackLine);
}

/// <summary>A career station. Listed newest first; an open end date means "current".</summary>
public sealed class Experience
{
  public const int MaxBullets = 12;

  private readonly List<ExperienceTranslation> translations = [];

  private Experience()
  {
  }

  public long Id { get; private set; }

  public string Company { get; private set; } = null!;

  public string? Location { get; private set; }

  public DateOnly StartDate { get; private set; }

  public DateOnly? EndDate { get; private set; }

  public int SortOrder { get; private set; }

  public bool IsVisible { get; private set; }

  /// <summary>A station without an end date is the current one.</summary>
  public bool IsCurrent => EndDate is null;

  public IReadOnlyList<ExperienceTranslation> Translations => translations;

  /// <summary>Creates a station from its details and the mandatory German texts.</summary>
  public static Experience Create(ExperienceDetails details, ExperienceTexts german)
  {
    var experience = new Experience();
    experience.UpdateDetails(details);
    experience.SetTexts(Locale.German, german);
    return experience;
  }

  /// <summary>Replaces the structural data after validating it as a whole.</summary>
  public void UpdateDetails(ExperienceDetails details)
  {
    Require.DateRange(details.StartDate, details.EndDate);
    Company = Require.Text(details.Company, "company", TextLimits.Name);
    Location = Require.OptionalText(details.Location, "location", TextLimits.Name);
    StartDate = details.StartDate;
    EndDate = details.EndDate;
    SortOrder = details.SortOrder;
    IsVisible = details.IsVisible;
  }

  /// <summary>Writes the texts of one locale; clearing every English field removes the English set.</summary>
  public void SetTexts(Locale locale, ExperienceTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => ExperienceTranslation.Create(locale, texts),
      translation => translation.Apply(texts));
}

/// <summary>The texts of a career station in one locale.</summary>
public sealed class ExperienceTranslation : ITranslation
{
  private ExperienceTranslation()
  {
  }

  public long Id { get; private set; }

  public long ExperienceId { get; private set; }

  public Locale Locale { get; private set; }

  public string? RoleTitle { get; private set; }

  public string? Summary { get; private set; }

  public string[] Bullets { get; private set; } = [];

  public string? StackLine { get; private set; }

  internal static ExperienceTranslation Create(Locale locale, ExperienceTexts texts)
  {
    var translation = new ExperienceTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(ExperienceTexts texts)
  {
    RoleTitle = Require.Translated(texts.RoleTitle, Locale, "roleTitle", TextLimits.Name, required: true);
    Summary = Require.Translated(texts.Summary, Locale, "summary", TextLimits.Paragraph);
    Bullets = Require.Lines(texts.Bullets, $"{Locale.Code}.bullets", TextLimits.Line, Experience.MaxBullets);
    StackLine = Require.Translated(texts.StackLine, Locale, "stackLine", TextLimits.Line);
  }
}
