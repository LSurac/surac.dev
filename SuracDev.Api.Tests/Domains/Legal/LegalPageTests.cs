using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Legal;

public sealed class LegalPageTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void A_legal_page_has_a_key_and_german_texts_stamped_with_the_time()
  {
    var page = LegalPage.Create(LegalPageKey.Imprint, new LegalTexts("Impressum", "## Angaben"), Now);

    Assert.Equal(LegalPageKey.Imprint, page.Key);
    var german = Assert.Single(page.Translations);
    Assert.Equal("Impressum", german.Title);
    Assert.Equal(Now, german.UpdatedAt);
  }

  [Fact]
  public void The_unknown_key_is_rejected()
  {
    Assert.Throws<DomainValidationException>(
      () => LegalPage.Create(LegalPageKey.Unknown, new LegalTexts("Titel", "Text"), Now));
  }

  [Fact]
  public void Title_and_body_are_required_in_german()
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => LegalPage.Create(LegalPageKey.Privacy, new LegalTexts("Datenschutz", " "), Now));

    Assert.Contains("de.bodyMarkdown", exception.Errors.Keys);
  }

  [Fact]
  public void Changing_a_locale_only_moves_the_timestamp_of_that_locale()
  {
    var page = LegalPage.Create(LegalPageKey.Privacy, new LegalTexts("Datenschutz", "Text"), Now);
    var later = Now.AddDays(3);

    page.SetTexts(Locale.English, new LegalTexts("Privacy", "Text"), later);

    Assert.Equal(Now, page.Translations.Single(t => t.Locale == Locale.German).UpdatedAt);
    Assert.Equal(later, page.Translations.Single(t => t.Locale == Locale.English).UpdatedAt);
  }

  [Fact]
  public void Saving_unchanged_texts_keeps_the_timestamp()
  {
    var page = LegalPage.Create(LegalPageKey.Privacy, new LegalTexts("Datenschutz", "Text"), Now);

    page.SetTexts(Locale.German, new LegalTexts("Datenschutz", "Text"), Now.AddDays(1));

    Assert.Equal(Now, Assert.Single(page.Translations).UpdatedAt);
  }
}
