using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Applications;

public sealed class ApplicationLinkTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  private static readonly LinkSettings Settings = new(
    AudienceProfile.Recruiter,
    GreetingDe: "Für das Team Beispiel",
    GreetingEn: null,
    ExpiresAt: Now.AddDays(90),
    UnlockFullPdf: true);

  private static ApplicationLink NewLink(LinkSettings? settings = null)
    => ApplicationLink.Create(applicationId: 1, settings ?? Settings, ApplicationLinkToken.Generate().Hash, Now);

  private static Document NewDocument(DocumentKind kind, DocumentVisibility visibility, string contentType)
    => Document.Create(
      new DocumentUpload("datei", contentType, 10, "x/y", new string('a', 64), kind, visibility, null, null),
      Now);

  [Fact]
  public void A_link_is_active_until_it_expires()
  {
    var link = NewLink();

    Assert.True(link.IsActiveAt(Now));
    Assert.True(link.IsActiveAt(Now.AddDays(90).AddTicks(-1)));
    Assert.False(link.IsActiveAt(Now.AddDays(90)));
  }

  [Fact]
  public void A_revoked_link_is_inactive_immediately()
  {
    var link = NewLink();

    link.Revoke();

    Assert.False(link.IsActiveAt(Now));
  }

  [Fact]
  public void A_revoked_link_cannot_be_changed_or_given_documents()
  {
    var link = NewLink();

    link.Revoke();

    Assert.Throws<DomainConflictException>(() => link.Update(Settings, Now));
    Assert.Throws<DomainConflictException>(() => link.AttachDocuments([]));
  }

  [Fact]
  public void The_expiry_must_lie_in_the_future()
  {
    var exception = Assert.Throws<DomainValidationException>(() => NewLink(Settings with { ExpiresAt = Now }));

    Assert.Contains("expiresAt", exception.Errors.Keys);
  }

  [Fact]
  public void The_unknown_profile_is_rejected()
  {
    Assert.Throws<DomainValidationException>(() => NewLink(Settings with { Profile = AudienceProfile.Unknown }));
  }

  [Fact]
  public void The_greeting_follows_the_locale_with_german_as_fallback()
  {
    var link = NewLink();

    Assert.Equal("Für das Team Beispiel", link.GreetingFor(Locale.English));
    Assert.Equal("Für das Team Beispiel", link.GreetingFor(Locale.German));
  }

  [Fact]
  public void Only_link_only_documents_can_be_attached()
  {
    var link = NewLink();
    var certificate = NewDocument(DocumentKind.Certificate, DocumentVisibility.LinkOnly, "application/pdf");
    var photo = NewDocument(DocumentKind.Photo, DocumentVisibility.Public, "image/png");

    Assert.Throws<DomainValidationException>(() => link.AttachDocuments([certificate, photo]));
    link.AttachDocuments([certificate]);

    Assert.Single(link.Documents);
  }

  [Fact]
  public void A_token_is_128_random_bits_in_base64url_and_stored_only_as_its_hash()
  {
    var first = ApplicationLinkToken.Generate();
    var second = ApplicationLinkToken.Generate();

    Assert.Equal(22, first.Token.Length);
    Assert.Matches("^[A-Za-z0-9_-]+$", first.Token);
    Assert.NotEqual(first.Token, second.Token);
    Assert.Equal(64, first.Hash.Length);
    Assert.Equal(first.Hash, ApplicationLinkToken.Hash(first.Token));
    Assert.DoesNotContain(first.Token, first.Hash, StringComparison.Ordinal);
  }
}
