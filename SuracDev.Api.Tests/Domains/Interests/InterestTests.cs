using SuracDev.Api.Domains.Interests.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Interests;

public sealed class InterestTests
{
  private static readonly InterestDetails Details = new(InterestKind.Chess, SortOrder: 1, IsVisible: true, ImageDocumentId: null);

  [Fact]
  public void A_new_interest_has_a_kind_and_a_german_title()
  {
    var interest = Interest.Create(Details, new InterestTexts("Schach", "Text"));

    Assert.Equal(InterestKind.Chess, interest.Kind);
    Assert.Equal("Schach", Assert.Single(interest.Translations).Title);
  }

  [Fact]
  public void The_unknown_kind_and_a_missing_german_title_are_rejected()
  {
    Assert.Throws<DomainValidationException>(
      () => Interest.Create(Details with { Kind = InterestKind.Unknown }, new InterestTexts("Titel", null)));
    Assert.Throws<DomainValidationException>(() => Interest.Create(Details, new InterestTexts(" ", "Text")));
  }

  [Fact]
  public void Details_and_english_texts_can_change()
  {
    var interest = Interest.Create(Details, new InterestTexts("Schach", null));

    interest.UpdateDetails(Details with { IsVisible = false, ImageDocumentId = 3 });
    interest.SetTexts(Locale.English, new InterestTexts("Chess", null));

    Assert.False(interest.IsVisible);
    Assert.Equal(3, interest.ImageDocumentId);
    Assert.Equal(2, interest.Translations.Count);
  }
}
