using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Applications.Domain;

/// <summary>What happened on a link. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum LinkEventKind
{
  Unknown = 0,
  Opened = 1,
  PdfDownloaded = 2,
  DocumentDownloaded = 3,
}

/// <summary>
/// A coarse event on an application link: kind, time, locale and, for a download, the document.
/// Never an IP address, user agent or referrer; that keeps the statistics free of visitor data.
/// </summary>
public sealed class LinkEvent
{
  private LinkEvent()
  {
  }

  public long Id { get; private set; }

  public long LinkId { get; private set; }

  public LinkEventKind Kind { get; private set; }

  public DateTimeOffset OccurredAt { get; private set; }

  public Locale Locale { get; private set; }

  /// <summary>Set for a document download and only then.</summary>
  public long? DocumentId { get; private set; }

  /// <summary>Records an event; a document download must name its document, nothing else may.</summary>
  public static LinkEvent Create(long linkId, LinkEventKind kind, Locale locale, long? documentId, DateTimeOffset now)
  {
    var known = Require.Known(kind, "kind");
    if ((known == LinkEventKind.DocumentDownloaded) != documentId.HasValue)
    {
      throw new DomainValidationException("documentId", "Only a document download names a document, and it must.");
    }

    return new LinkEvent { LinkId = linkId, Kind = known, Locale = locale, DocumentId = documentId, OccurredAt = now };
  }
}
