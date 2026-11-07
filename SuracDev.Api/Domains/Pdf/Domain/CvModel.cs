using System.Globalization;
using System.Text;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Pdf.Domain;

/// <summary>
/// Everything the CV prints, already in one locale and already formatted. Building it is separate
/// from laying it out, so the visibility rules are decided once and the layout only places text.
/// </summary>
public sealed record CvModel(
  Locale Locale,
  string FullName,
  string TitleLine,
  IReadOnlyList<string> ContactLines,
  string? Intro,
  IReadOnlyList<CvEntry> Experience,
  IReadOnlyList<CvEntry> Education,
  IReadOnlyList<CvSkillGroup> Skills,
  IReadOnlyList<CvProject> Projects);

/// <summary>A career station or an education entry: period, title, organisation, text, bullets, detail line.</summary>
public sealed record CvEntry(
  string Period,
  string Title,
  string Organisation,
  string? Summary,
  IReadOnlyList<string> Bullets,
  string? Detail);

/// <summary>A skill group as one line: the name and its formatted skills.</summary>
public sealed record CvSkillGroup(string Name, IReadOnlyList<string> Skills);

/// <summary>A featured project: title, teaser and stack line.</summary>
public sealed record CvProject(string Title, string Teaser, string? StackLine);

/// <summary>The fixed texts of the CV per locale: standard section headings an ATS recognises.</summary>
public sealed record CvTexts(
  string DocumentPrefix,
  string Profile,
  string Experience,
  string Education,
  string Skills,
  string Projects,
  string OpenEnd,
  string Since,
  string Grade)
{
  private static readonly CvTexts German = new(
    "Lebenslauf", "Profil", "Berufserfahrung", "Ausbildung", "Kenntnisse", "Projekte", "heute", "seit", "Note");

  private static readonly CvTexts English = new(
    "CV", "Profile", "Experience", "Education", "Skills", "Projects", "present", "since", "Grade");

  /// <summary>The texts of <paramref name="locale"/>.</summary>
  public static CvTexts For(Locale locale) => locale.IsPrimary ? German : English;
}

/// <summary>Formatting rules of the CV, kept pure so they are tested without rendering.</summary>
public static class CvFormat
{
  /// <summary>"09/2022 – heute": month and year of both ends, an open end named per locale.</summary>
  public static string Period(DateOnly start, DateOnly? end, Locale locale)
    => $"{MonthYear(start)} – {(end is { } last ? MonthYear(last) : CvTexts.For(locale).OpenEnd)}";

  /// <summary>"C# (seit 2019)", "VB.NET (Legacy, since 2019)": the since year and qualifier, never a level.</summary>
  public static string Skill(string name, int? sinceYear, string? qualifier, Locale locale)
  {
    var since = sinceYear is { } year ? $"{CvTexts.For(locale).Since} {year}" : null;
    var details = new[] { qualifier, since }.Where(part => !string.IsNullOrWhiteSpace(part)).ToList();
    return details.Count == 0 ? name : $"{name} ({string.Join(", ", details)})";
  }

  /// <summary>"Lebenslauf_Max_Mustermann.pdf" / "CV_Max_Mustermann.pdf": ASCII only, safe for every mail client.</summary>
  public static string FileName(string fullName, Locale locale)
  {
    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(ToAscii)
      .Where(part => part.Length > 0);
    return $"{CvTexts.For(locale).DocumentPrefix}_{string.Join('_', parts)}.pdf";
  }

  private static string MonthYear(DateOnly date)
    => date.ToString("MM/yyyy", CultureInfo.InvariantCulture);

  private static string ToAscii(string text)
  {
    var result = new StringBuilder();
    foreach (var character in text)
    {
      result.Append(character switch
      {
        'ä' => "ae",
        'ö' => "oe",
        'ü' => "ue",
        'Ä' => "Ae",
        'Ö' => "Oe",
        'Ü' => "Ue",
        'ß' => "ss",
        _ when char.IsAsciiLetterOrDigit(character) || character == '-' => character.ToString(),
        _ => string.Empty,
      });
    }

    return result.ToString();
  }
}
