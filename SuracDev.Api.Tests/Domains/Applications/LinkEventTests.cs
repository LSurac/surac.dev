using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Applications;

public sealed class LinkEventTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void An_event_records_kind_time_and_locale_only()
  {
    var linkEvent = LinkEvent.Create(5, LinkEventKind.Opened, Locale.English, documentId: null, Now);

    Assert.Equal(5, linkEvent.LinkId);
    Assert.Equal(Locale.English, linkEvent.Locale);
    Assert.Null(linkEvent.DocumentId);
  }

  [Fact]
  public void A_document_download_names_the_document_and_nothing_else_does()
  {
    Assert.Throws<DomainValidationException>(
      () => LinkEvent.Create(5, LinkEventKind.DocumentDownloaded, Locale.German, documentId: null, Now));
    Assert.Throws<DomainValidationException>(
      () => LinkEvent.Create(5, LinkEventKind.PdfDownloaded, Locale.German, documentId: 3, Now));
    Assert.Equal(3, LinkEvent.Create(5, LinkEventKind.DocumentDownloaded, Locale.German, 3, Now).DocumentId);
  }

  [Fact]
  public void The_unknown_kind_is_rejected()
  {
    Assert.Throws<DomainValidationException>(
      () => LinkEvent.Create(5, LinkEventKind.Unknown, Locale.German, documentId: null, Now));
  }
}
