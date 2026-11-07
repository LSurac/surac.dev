using SuracDev.Api.Domains.Profiles.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Profiles;

public sealed class ProfileTests
{
  private static readonly ProfileContact Contact = new(
    FullName: " Max Mustermann ",
    Email: "max@example.org",
    Phone: "+49 000 000000",
    Location: "Musterstadt",
    AddressLines: ["Musterstraße 1", " ", "00000 Musterstadt"],
    GitHubUrl: "https://github.com/example",
    LinkedInUrl: null);

  private static readonly ProfileTexts German = new(
    TitleLine: "Fullstack-Entwickler · .NET & Angular",
    Headline: "Beispielüberschrift.",
    Intro: "Beispieltext.",
    AvailabilityLine: null);

  [Fact]
  public void A_new_profile_carries_its_contact_data_and_the_german_texts()
  {
    var profile = Profile.Create(Contact, German);

    Assert.Equal("Max Mustermann", profile.FullName);
    Assert.Equal(["Musterstraße 1", "00000 Musterstadt"], profile.AddressLines);
    Assert.Null(profile.LinkedInUrl);
    var german = Assert.Single(profile.Translations);
    Assert.Equal(Locale.German, german.Locale);
    Assert.Equal("Beispielüberschrift.", german.Headline);
  }

  [Fact]
  public void The_title_line_and_the_headline_are_required_in_german()
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => Profile.Create(Contact, German with { TitleLine = " " }));

    Assert.Contains("de.titleLine", exception.Errors.Keys);
    Assert.Throws<DomainValidationException>(() => Profile.Create(Contact, German with { Headline = null }));
  }

  [Fact]
  public void Name_email_and_location_are_required()
  {
    Assert.Throws<DomainValidationException>(() => Profile.Create(Contact with { FullName = "" }, German));
    Assert.Throws<DomainValidationException>(() => Profile.Create(Contact with { Email = "kein-mail" }, German));
    Assert.Throws<DomainValidationException>(() => Profile.Create(Contact with { Location = " " }, German));
  }

  [Fact]
  public void English_texts_are_optional_and_may_be_partial()
  {
    var profile = Profile.Create(Contact, German);

    profile.SetTexts(Locale.English, new ProfileTexts("Full-stack developer", null, null, null));

    var english = profile.Translations.Single(translation => translation.Locale == Locale.English);
    Assert.Equal("Full-stack developer", english.TitleLine);
    Assert.Null(english.Headline);
  }

  [Fact]
  public void Clearing_every_english_text_removes_the_english_translation()
  {
    var profile = Profile.Create(Contact, German);
    profile.SetTexts(Locale.English, new ProfileTexts("Full-stack developer", null, null, null));

    profile.SetTexts(Locale.English, new ProfileTexts(" ", null, "", null));

    Assert.Single(profile.Translations);
  }

  [Fact]
  public void Updating_german_texts_changes_the_existing_translation()
  {
    var profile = Profile.Create(Contact, German);

    profile.SetTexts(Locale.German, German with { Headline = "Neu." });

    Assert.Equal("Neu.", Assert.Single(profile.Translations).Headline);
  }

  [Fact]
  public void The_fact_bar_keeps_the_given_order_and_both_labels()
  {
    var profile = Profile.Create(Contact, German);

    profile.ReplaceFacts([new FactInput("5", "Jahre", "Years"), new FactInput("1,8", "Abschluss", null)]);

    Assert.Equal(["5", "1,8"], profile.Facts.Select(fact => fact.Value));
    Assert.Equal([0, 1], profile.Facts.Select(fact => fact.SortOrder));
    Assert.Equal(2, profile.Facts[0].Translations.Count);
    Assert.Single(profile.Facts[1].Translations);
  }

  [Fact]
  public void The_fact_bar_has_at_most_four_cells()
  {
    var profile = Profile.Create(Contact, German);
    var five = Enumerable.Range(1, 5).Select(i => new FactInput($"{i}", "Label", null)).ToList();

    var exception = Assert.Throws<DomainValidationException>(() => profile.ReplaceFacts(five));

    Assert.Contains("facts", exception.Errors.Keys);
  }

  [Fact]
  public void A_fact_needs_a_value_and_a_german_label()
  {
    var profile = Profile.Create(Contact, German);

    Assert.Throws<DomainValidationException>(() => profile.ReplaceFacts([new FactInput(" ", "Label", null)]));
    Assert.Throws<DomainValidationException>(() => profile.ReplaceFacts([new FactInput("5", null, "Years")]));
  }

  [Fact]
  public void The_photo_is_a_reference_to_a_document()
  {
    var profile = Profile.Create(Contact, German);

    profile.SetPhoto(42);
    Assert.Equal(42, profile.PhotoDocumentId);

    profile.SetPhoto(null);
    Assert.Null(profile.PhotoDocumentId);
  }
}
