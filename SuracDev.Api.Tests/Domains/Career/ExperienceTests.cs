using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Career;

public sealed class ExperienceTests
{
  private static readonly ExperienceDetails Details = new(
    Company: " Beispiel GmbH ",
    Location: "Musterstadt",
    StartDate: new DateOnly(2022, 9, 1),
    EndDate: null,
    SortOrder: 0,
    IsVisible: true);

  private static readonly ExperienceTexts German = new(
    RoleTitle: "Softwareentwickler",
    Summary: "Beispieltätigkeit.",
    Bullets: [" Erster Punkt ", "", "Zweiter Punkt"],
    StackLine: ".NET · Angular");

  [Fact]
  public void A_new_experience_carries_its_period_and_german_texts()
  {
    var experience = Experience.Create(Details, German);

    Assert.Equal("Beispiel GmbH", experience.Company);
    Assert.True(experience.IsCurrent);
    var german = Assert.Single(experience.Translations);
    Assert.Equal(["Erster Punkt", "Zweiter Punkt"], german.Bullets);
  }

  [Fact]
  public void The_company_and_the_german_role_title_are_required()
  {
    Assert.Throws<DomainValidationException>(() => Experience.Create(Details with { Company = " " }, German));
    var exception = Assert.Throws<DomainValidationException>(
      () => Experience.Create(Details, German with { RoleTitle = null }));

    Assert.Contains("de.roleTitle", exception.Errors.Keys);
  }

  [Fact]
  public void The_end_date_may_not_precede_the_start_date()
  {
    Assert.Throws<DomainValidationException>(
      () => Experience.Create(Details with { EndDate = new DateOnly(2022, 8, 31) }, German));
  }

  [Fact]
  public void An_ended_experience_is_not_current()
  {
    var experience = Experience.Create(Details with { EndDate = new DateOnly(2024, 4, 30) }, German);

    Assert.False(experience.IsCurrent);
  }

  [Fact]
  public void English_texts_can_be_added_and_removed_again()
  {
    var experience = Experience.Create(Details, German);

    experience.SetTexts(Locale.English, new ExperienceTexts("Software developer", null, [], null));
    Assert.Equal(2, experience.Translations.Count);

    experience.SetTexts(Locale.English, new ExperienceTexts(null, " ", [" "], null));
    Assert.Single(experience.Translations);
  }

  [Fact]
  public void Updating_the_details_validates_them_again()
  {
    var experience = Experience.Create(Details, German);

    experience.UpdateDetails(Details with { Company = "Andere AG", IsVisible = false });

    Assert.Equal("Andere AG", experience.Company);
    Assert.False(experience.IsVisible);
    Assert.Throws<DomainValidationException>(() => experience.UpdateDetails(Details with { Company = "" }));
  }

  [Fact]
  public void An_experience_has_at_most_twelve_bullets()
  {
    var bullets = Enumerable.Range(1, 13).Select(i => $"Punkt {i}").ToList();

    Assert.Throws<DomainValidationException>(
      () => Experience.Create(Details, German with { Bullets = bullets }));
  }
}
