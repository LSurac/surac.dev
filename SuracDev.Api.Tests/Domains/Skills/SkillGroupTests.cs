using SuracDev.Api.Domains.Skills.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Skills;

public sealed class SkillGroupTests
{
  [Fact]
  public void A_new_group_has_a_german_name_and_its_order()
  {
    var group = SkillGroup.Create(2, " Backend ", null);

    Assert.Equal(2, group.SortOrder);
    Assert.Equal("Backend", Assert.Single(group.Translations).Name);
  }

  [Fact]
  public void The_german_name_is_required()
  {
    var exception = Assert.Throws<DomainValidationException>(() => SkillGroup.Create(0, " ", "Backend"));

    Assert.Contains("de.name", exception.Errors.Keys);
  }

  [Fact]
  public void Renaming_can_add_and_remove_the_english_name()
  {
    var group = SkillGroup.Create(0, "Betrieb", null);

    group.Rename("Betrieb", "Operations");
    Assert.Equal(2, group.Translations.Count);

    group.Rename("Betrieb", null);
    Assert.Single(group.Translations);
  }

  [Fact]
  public void Skills_keep_the_given_order_with_since_year_and_optional_qualifier()
  {
    var group = SkillGroup.Create(0, "Backend", null);

    group.ReplaceSkills(
    [
      new SkillInput(" C# ", 2019, null, null),
      new SkillInput("VB.NET", 2019, "Legacy", "legacy"),
    ]);

    Assert.Equal(["C#", "VB.NET"], group.Skills.Select(skill => skill.Name));
    Assert.Equal([0, 1], group.Skills.Select(skill => skill.SortOrder));
    Assert.Null(group.Skills[0].Translations.Single().Qualifier);
    Assert.Equal(2, group.Skills[1].Translations.Count);
  }

  [Fact]
  public void A_skill_needs_a_name_and_a_plausible_year()
  {
    var group = SkillGroup.Create(0, "Backend", null);

    Assert.Throws<DomainValidationException>(() => group.ReplaceSkills([new SkillInput(" ", null, null, null)]));
    Assert.Throws<DomainValidationException>(() => group.ReplaceSkills([new SkillInput("C#", 1900, null, null)]));
  }

  [Fact]
  public void A_skill_name_appears_once_per_group()
  {
    var group = SkillGroup.Create(0, "Backend", null);

    var exception = Assert.Throws<DomainValidationException>(
      () => group.ReplaceSkills([new SkillInput("C#", null, null, null), new SkillInput("c#", null, null, null)]));

    Assert.Contains("skills", exception.Errors.Keys);
  }

  [Fact]
  public void Every_skill_keeps_a_german_translation_even_without_a_qualifier()
  {
    var group = SkillGroup.Create(0, "Backend", null);

    group.ReplaceSkills([new SkillInput("C#", null, null, null)]);

    Assert.Equal(Locale.German, Assert.Single(group.Skills[0].Translations).Locale);
  }
}
