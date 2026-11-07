namespace SuracDev.Api.Domains.Career.Contracts;

/// <summary>A career station as the open site shows it, in one locale.</summary>
/// <param name="Id">Stable identifier, for list keys.</param>
/// <param name="Company">The employer.</param>
/// <param name="Location">City or region, if given.</param>
/// <param name="StartDate">First day of the station.</param>
/// <param name="EndDate">Last day, or null for the current station.</param>
/// <param name="RoleTitle">The role, in the requested locale or German.</param>
/// <param name="Summary">One paragraph about the station.</param>
/// <param name="Bullets">Short achievement lines in display order.</param>
/// <param name="StackLine">The technologies used, as one line.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for them.</param>
public sealed record PublicExperience(
  long Id,
  string Company,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  string RoleTitle,
  string? Summary,
  IReadOnlyList<string> Bullets,
  string? StackLine,
  IReadOnlyList<string> FallbackFields);

/// <summary>A career station for the admin editor, with both locales and its visibility.</summary>
/// <param name="Id">The station's identifier.</param>
/// <param name="Company">Required.</param>
/// <param name="Location">Optional city or region.</param>
/// <param name="StartDate">First day of the station.</param>
/// <param name="EndDate">Last day, or null for the current station; never before the start.</param>
/// <param name="SortOrder">Tie-breaker between stations with the same start date.</param>
/// <param name="IsVisible">Hidden stations stay in the admin and never reach the open site or the PDF.</param>
/// <param name="De">The German texts; the role title is required.</param>
/// <param name="En">The English texts, or null when there are none.</param>
public sealed record AdminExperience(
  long Id,
  string Company,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  int SortOrder,
  bool IsVisible,
  ExperienceTextsForm De,
  ExperienceTextsForm? En);

/// <summary>The writable fields of a career station, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Company">Required.</param>
/// <param name="Location">Optional city or region.</param>
/// <param name="StartDate">First day of the station.</param>
/// <param name="EndDate">Last day, or null for the current station; never before the start.</param>
/// <param name="SortOrder">Tie-breaker between stations with the same start date.</param>
/// <param name="IsVisible">Whether the open site and the PDF show the station.</param>
/// <param name="De">The German texts; the role title is required.</param>
/// <param name="En">The English texts, or null to remove them.</param>
public sealed record ExperienceForm(
  string Company,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  int SortOrder,
  bool IsVisible,
  ExperienceTextsForm De,
  ExperienceTextsForm? En);

/// <summary>The translatable texts of a career station in one locale.</summary>
/// <param name="RoleTitle">The role; required in German.</param>
/// <param name="Summary">Optional paragraph.</param>
/// <param name="Bullets">At most twelve short lines; blank lines are dropped.</param>
/// <param name="StackLine">Optional technology line.</param>
public sealed record ExperienceTextsForm(
  string? RoleTitle,
  string? Summary,
  IReadOnlyList<string> Bullets,
  string? StackLine);

/// <summary>An education entry as the open site shows it, in one locale.</summary>
/// <param name="Id">Stable identifier, for list keys.</param>
/// <param name="Institution">School, chamber or university.</param>
/// <param name="Location">City or region, if given.</param>
/// <param name="StartDate">First day.</param>
/// <param name="EndDate">Last day, or null while ongoing.</param>
/// <param name="Grade">Free-text grade ("1,8"), if given.</param>
/// <param name="Degree">The degree, in the requested locale or German.</param>
/// <param name="Summary">Optional paragraph.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for them.</param>
public sealed record PublicEducation(
  long Id,
  string Institution,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  string? Grade,
  string Degree,
  string? Summary,
  IReadOnlyList<string> FallbackFields);

/// <summary>An education entry for the admin editor, with both locales and its visibility.</summary>
/// <param name="Id">The entry's identifier.</param>
/// <param name="Institution">Required.</param>
/// <param name="Location">Optional city or region.</param>
/// <param name="StartDate">First day.</param>
/// <param name="EndDate">Last day, or null while ongoing; never before the start.</param>
/// <param name="Grade">Optional free-text grade, at most 40 characters.</param>
/// <param name="SortOrder">Tie-breaker between entries with the same start date.</param>
/// <param name="IsVisible">Hidden entries never reach the open site or the PDF.</param>
/// <param name="De">The German texts; the degree is required.</param>
/// <param name="En">The English texts, or null when there are none.</param>
public sealed record AdminEducation(
  long Id,
  string Institution,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  string? Grade,
  int SortOrder,
  bool IsVisible,
  EducationTextsForm De,
  EducationTextsForm? En);

/// <summary>The writable fields of an education entry, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Institution">Required.</param>
/// <param name="Location">Optional city or region.</param>
/// <param name="StartDate">First day.</param>
/// <param name="EndDate">Last day, or null while ongoing; never before the start.</param>
/// <param name="Grade">Optional free-text grade, at most 40 characters.</param>
/// <param name="SortOrder">Tie-breaker between entries with the same start date.</param>
/// <param name="IsVisible">Whether the open site and the PDF show the entry.</param>
/// <param name="De">The German texts; the degree is required.</param>
/// <param name="En">The English texts, or null to remove them.</param>
public sealed record EducationForm(
  string Institution,
  string? Location,
  DateOnly StartDate,
  DateOnly? EndDate,
  string? Grade,
  int SortOrder,
  bool IsVisible,
  EducationTextsForm De,
  EducationTextsForm? En);

/// <summary>The translatable texts of an education entry in one locale.</summary>
/// <param name="Degree">The degree; required in German.</param>
/// <param name="Summary">Optional paragraph.</param>
public sealed record EducationTextsForm(string? Degree, string? Summary);
