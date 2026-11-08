using SuracDev.Api.Domains.Pdf.Application;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Pdf;

public sealed class CvModelTests
{
  private static readonly PublicProfile Profile = new(
    "Max Mustermann",
    "max.mustermann@example.org",
    "Musterstadt",
    "https://github.com/example",
    null,
    null,
    "Fullstack-Entwickler",
    "Überschrift",
    null,
    null,
    [],
    []);

  private static IReadOnlyList<string> ContactLines(CvPrivateContact? privateContact)
    => CvService.BuildModel(Locale.German, Profile, privateContact, [], [], [], []).ContactLines;

  [Fact]
  public void The_public_cv_names_e_mail_location_and_links_only()
  {
    Assert.Equal(["max.mustermann@example.org", "Musterstadt", "github.com/example"], ContactLines(null));
  }

  [Fact]
  public void The_full_cv_adds_the_phone_and_replaces_the_location_with_the_street_address()
  {
    var lines = ContactLines(new CvPrivateContact("+49 000 111222", ["Musterstraße 1", "12345 Musterstadt"]));

    Assert.Equal(
      ["max.mustermann@example.org", "+49 000 111222", "Musterstraße 1, 12345 Musterstadt", "github.com/example"],
      lines);
  }

  [Fact]
  public void The_full_cv_keeps_the_location_when_no_street_address_is_maintained()
  {
    var lines = ContactLines(new CvPrivateContact(null, []));

    Assert.Equal(["max.mustermann@example.org", "Musterstadt", "github.com/example"], lines);
  }
}
