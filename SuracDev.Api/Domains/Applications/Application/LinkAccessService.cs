using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Pdf.Application;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>
/// What a recipient does with an application link: open it, download the CV, download an attached
/// document. Each successful step records a <see cref="LinkEvent"/>; unknown, expired and revoked
/// tokens all answer the same not-found and record nothing (docs/security-and-privacy.md).
/// </summary>
public sealed class LinkAccessService(
  SuracDevDbContext dbContext,
  CvService cvService,
  DocumentService documentService,
  TimeProvider timeProvider)
{
  /// <summary>The link's greeting, section order and downloads in <paramref name="locale"/>; records an
  /// opening.</summary>
  /// <exception cref="DomainNotFoundException">The token names no active link.</exception>
  public async Task<PublicApplicationLink> OpenAsync(string token, Locale locale, CancellationToken cancellationToken)
  {
    var link = await FindActiveAsync(token, cancellationToken);
    var documentIds = link.Documents.Select(document => document.DocumentId).ToList();
    var documents = await dbContext.Documents.AsNoTracking()
      .Include(document => document.Translations)
      .Where(document => documentIds.Contains(document.Id))
      .OrderBy(document => document.Kind == DocumentKind.CoverLetter ? 0 : 1)
      .ThenBy(document => document.Id)
      .ToListAsync(cancellationToken);

    await RecordAsync(link, LinkEventKind.Opened, locale, null, cancellationToken);
    return new PublicApplicationLink(
      link.GreetingFor(locale),
      SectionOrder.For(link.Profile),
      link.UnlockFullPdf,
      documents.Select(document => ToPublic(document, locale)).ToList());
  }

  /// <summary>The CV the link unlocks: the full variant with <c>UnlockFullPdf</c>, the public one otherwise; records a
  /// download.</summary>
  /// <exception cref="DomainNotFoundException">The token names no active link.</exception>
  public async Task<CvFile> RenderCvAsync(string token, Locale locale, CancellationToken cancellationToken)
  {
    var link = await FindActiveAsync(token, cancellationToken);
    var cv = link.UnlockFullPdf
      ? await cvService.RenderFullAsync(locale, cancellationToken)
      : await cvService.RenderPublicAsync(locale, cancellationToken);
    await RecordAsync(link, LinkEventKind.PdfDownloaded, locale, null, cancellationToken);
    return cv;
  }

  /// <summary>A document attached to the link; records a download.</summary>
  /// <exception cref="DomainNotFoundException">The token names no active link, or the link has no such
  /// document.</exception>
  public async Task<DocumentContent> OpenDocumentAsync(
    string token,
    long documentId,
    Locale locale,
    CancellationToken cancellationToken)
  {
    var link = await FindActiveAsync(token, cancellationToken);
    var content = await documentService.OpenAttachedAsync(link.Id, documentId, cancellationToken);
    await RecordAsync(link, LinkEventKind.DocumentDownloaded, locale, documentId, cancellationToken);
    return content;
  }

  private async Task<ApplicationLink> FindActiveAsync(string token, CancellationToken cancellationToken)
  {
    var hash = ApplicationLinkToken.Hash(token);
    var link = await dbContext.ApplicationLinks.AsNoTracking()
      .Include(candidate => candidate.Documents)
      .SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
    return link is not null && link.IsActiveAt(timeProvider.GetUtcNow())
      ? link
      : throw new DomainNotFoundException("The link does not exist.");
  }

  private async Task RecordAsync(
    ApplicationLink link,
    LinkEventKind kind,
    Locale locale,
    long? documentId,
    CancellationToken cancellationToken)
  {
    dbContext.LinkEvents.Add(LinkEvent.Create(link.Id, kind, locale, documentId, timeProvider.GetUtcNow()));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static PublicLinkDocument ToPublic(Document document, Locale locale)
  {
    var label = new LocalizedText<DocumentTranslation>(document.Translations, locale).Text(t => t.Label, "label");
    return new PublicLinkDocument(document.Id, label ?? document.FileName, document.FileName, document.Kind);
  }
}
