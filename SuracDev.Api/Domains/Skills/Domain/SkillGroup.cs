using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Skills.Domain;

/// <summary>One skill as the admin enters it. The name is a technology name and is not translated.</summary>
public sealed record SkillInput(string? Name, int? SinceYear, string? QualifierDe, string? QualifierEn);

/// <summary>
/// A titled group of skills (Backend, Frontend, Betrieb). The group owns its skills: they are edited
/// and ordered together. Skills carry a "since year", never a level or a percentage.
/// </summary>
public sealed class SkillGroup
{
  public const int MaxSkills = 30;

  private readonly List<SkillGroupTranslation> translations = [];
  private readonly List<Skill> skills = [];

  private SkillGroup()
  {
  }

  public long Id { get; private set; }

  public int SortOrder { get; private set; }

  public IReadOnlyList<SkillGroupTranslation> Translations => translations;

  public IReadOnlyList<Skill> Skills => skills;

  /// <summary>Creates a group with its position and the mandatory German name.</summary>
  public static SkillGroup Create(int sortOrder, string? nameDe, string? nameEn)
  {
    var group = new SkillGroup { SortOrder = sortOrder };
    group.Rename(nameDe, nameEn);
    return group;
  }

  /// <summary>Moves the group to another position among the groups.</summary>
  public void MoveTo(int sortOrder) => SortOrder = sortOrder;

  /// <summary>Sets both names; an empty English name removes the English translation.</summary>
  public void Rename(string? nameDe, string? nameEn)
  {
    SetName(Locale.German, nameDe);
    SetName(Locale.English, nameEn);
  }

  /// <summary>Replaces the skills with the given list, in that order; names are unique per group.</summary>
  public void ReplaceSkills(IReadOnlyList<SkillInput> input)
  {
    if (input.Count > MaxSkills)
    {
      throw new DomainValidationException("skills", $"At most {MaxSkills} skills per group.");
    }

    var replacement = input.Select((skill, index) => Skill.Create(index, skill)).ToList();
    var duplicate = replacement
      .GroupBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
      .FirstOrDefault(group => group.Count() > 1);
    if (duplicate is not null)
    {
      throw new DomainValidationException("skills", $"'{duplicate.Key}' appears more than once.");
    }

    skills.Clear();
    skills.AddRange(replacement);
  }

  private void SetName(Locale locale, string? name)
    => TranslationList.Set(
      translations,
      locale,
      string.IsNullOrWhiteSpace(name),
      () => SkillGroupTranslation.Create(locale, name),
      translation => translation.Apply(name));
}

/// <summary>The name of a skill group in one locale.</summary>
public sealed class SkillGroupTranslation : ITranslation
{
  private SkillGroupTranslation()
  {
  }

  public long Id { get; private set; }

  public long SkillGroupId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Name { get; private set; }

  internal static SkillGroupTranslation Create(Locale locale, string? name)
  {
    var translation = new SkillGroupTranslation { Locale = locale };
    translation.Apply(name);
    return translation;
  }

  internal void Apply(string? name)
    => Name = Require.Translated(name, Locale, "name", TextLimits.Name, required: true);
}

/// <summary>A technology with the year since it is used and an optional qualifier ("Legacy").</summary>
public sealed class Skill
{
  public const int MaxNameLength = 80;

  private readonly List<SkillTranslation> translations = [];

  private Skill()
  {
  }

  public long Id { get; private set; }

  public long SkillGroupId { get; private set; }

  public int SortOrder { get; private set; }

  public string Name { get; private set; } = null!;

  public int? SinceYear { get; private set; }

  /// <summary>German always exists (possibly without a qualifier), so the reader has its fallback.</summary>
  public IReadOnlyList<SkillTranslation> Translations => translations;

  internal static Skill Create(int sortOrder, SkillInput input)
  {
    var skill = new Skill
    {
      SortOrder = sortOrder,
      Name = Require.Text(input.Name, "skills.name", MaxNameLength),
      SinceYear = Require.Year(input.SinceYear, "skills.sinceYear"),
    };
    skill.SetQualifier(Locale.German, input.QualifierDe);
    skill.SetQualifier(Locale.English, input.QualifierEn);
    return skill;
  }

  private void SetQualifier(Locale locale, string? qualifier)
    => TranslationList.Set(
      translations,
      locale,
      string.IsNullOrWhiteSpace(qualifier),
      () => SkillTranslation.Create(locale, qualifier),
      translation => translation.Apply(qualifier));
}

/// <summary>The qualifier of a skill in one locale.</summary>
public sealed class SkillTranslation : ITranslation
{
  private SkillTranslation()
  {
  }

  public long Id { get; private set; }

  public long SkillId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Qualifier { get; private set; }

  internal static SkillTranslation Create(Locale locale, string? qualifier)
  {
    var translation = new SkillTranslation { Locale = locale };
    translation.Apply(qualifier);
    return translation;
  }

  internal void Apply(string? qualifier)
    => Qualifier = Require.Translated(qualifier, Locale, "skills.qualifier", TextLimits.Name);
}
