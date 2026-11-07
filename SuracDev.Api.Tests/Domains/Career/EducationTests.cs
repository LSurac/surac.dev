using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Career;

public sealed class EducationTests
{
  private static readonly EducationDetails Details = new(
    Institution: "Beispiel-Berufsschule",
    Location: "Musterstadt",
    StartDate: new DateOnly(2019, 9, 1),
    EndDate: new DateOnly(2022, 6, 30),
    Grade: " 1,8 ",
    SortOrder: 0,
    IsVisible: true);

  private static readonly EducationTexts German = new(Degree: "Fachinformatiker", Summary: null);

  [Fact]
  public void A_new_education_carries_its_grade_and_german_degree()
  {
    var education = Education.Create(Details, German);

    Assert.Equal("1,8", education.Grade);
    Assert.Equal("Fachinformatiker", Assert.Single(education.Translations).Degree);
  }

  [Fact]
  public void The_institution_and_the_german_degree_are_required()
  {
    Assert.Throws<DomainValidationException>(() => Education.Create(Details with { Institution = null }, German));
    var exception = Assert.Throws<DomainValidationException>(
      () => Education.Create(Details, German with { Degree = " " }));

    Assert.Contains("de.degree", exception.Errors.Keys);
  }

  [Fact]
  public void The_end_date_may_not_precede_the_start_date()
  {
    Assert.Throws<DomainValidationException>(
      () => Education.Create(Details with { EndDate = new DateOnly(2019, 1, 1) }, German));
  }

  [Fact]
  public void English_texts_are_optional()
  {
    var education = Education.Create(Details, German);

    education.SetTexts(Locale.English, new EducationTexts("IT specialist", null));
    education.UpdateDetails(Details with { Grade = null });

    Assert.Equal(2, education.Translations.Count);
    Assert.Null(education.Grade);
  }
}
