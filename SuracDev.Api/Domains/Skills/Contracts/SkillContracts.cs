namespace SuracDev.Api.Domains.Skills.Contracts;

/// <summary>A skill group of the open site with its skills, in one locale.</summary>
/// <param name="Id">Stable identifier, for list keys.</param>
/// <param name="Name">The group name in the requested locale or German.</param>
/// <param name="Skills">The skills in display order.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for them.</param>
public sealed record PublicSkillGroup(
  long Id,
  string Name,
  IReadOnlyList<PublicSkill> Skills,
  IReadOnlyList<string> FallbackFields);

/// <summary>A skill as the open site shows it: a technology, since when, and an optional qualifier. Never a level.</summary>
/// <param name="Name">The technology name; not translated.</param>
/// <param name="SinceYear">The year since it is used, if given.</param>
/// <param name="Qualifier">A short qualifier ("Legacy"), in the requested locale or German.</param>
public sealed record PublicSkill(string Name, int? SinceYear, string? Qualifier);

/// <summary>A skill group for the admin editor, with both names and its skills.</summary>
/// <param name="Id">The group's identifier.</param>
/// <param name="SortOrder">Position among the groups.</param>
/// <param name="NameDe">Required German name.</param>
/// <param name="NameEn">Optional English name.</param>
/// <param name="Skills">The skills in display order.</param>
public sealed record AdminSkillGroup(
  long Id,
  int SortOrder,
  string NameDe,
  string? NameEn,
  IReadOnlyList<SkillForm> Skills);

/// <summary>The writable fields of a skill group, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="SortOrder">Position among the groups.</param>
/// <param name="NameDe">Required German name.</param>
/// <param name="NameEn">Optional English name; empty falls back to German.</param>
/// <param name="Skills">At most 30 skills in display order; names are unique per group (ignoring case).</param>
public sealed record SkillGroupForm(
  int SortOrder,
  string NameDe,
  string? NameEn,
  IReadOnlyList<SkillForm> Skills);

/// <summary>One skill with both qualifiers.</summary>
/// <param name="Name">Required technology name, at most 80 characters.</param>
/// <param name="SinceYear">Optional year between 1950 and 2100.</param>
/// <param name="QualifierDe">Optional German qualifier.</param>
/// <param name="QualifierEn">Optional English qualifier.</param>
public sealed record SkillForm(string Name, int? SinceYear, string? QualifierDe, string? QualifierEn);
