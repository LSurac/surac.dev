using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>
/// The admin's use cases of application links: list, create (the only moment the clear token
/// exists), change and revoke. A link is always addressed under its application.
/// </summary>
public sealed class ApplicationLinkService(SuracDevDbContext dbContext, TimeProvider timeProvider)
{
  /// <summary>The links of an application, newest first.</summary>
  /// <exception cref="DomainNotFoundException">No application has this id.</exception>
  public async Task<IReadOnlyList<AdminApplicationLink>> ListAsync(long applicationId, CancellationToken cancellationToken)
  {
    await EnsureApplicationExistsAsync(applicationId, cancellationToken);
    var links = await dbContext.ApplicationLinks.AsNoTracking()
      .Include(link => link.Documents)
      .Where(link => link.ApplicationId == applicationId)
      .OrderByDescending(link => link.CreatedAt)
      .ThenByDescending(link => link.Id)
      .ToListAsync(cancellationToken);
    var now = timeProvider.GetUtcNow();
    return links.Select(link => ToAdmin(link, now)).ToList();
  }

  /// <summary>One link of an application.</summary>
  /// <exception cref="DomainNotFoundException">The application has no link with this id.</exception>
  public async Task<AdminApplicationLink> GetAsync(long applicationId, long linkId, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(applicationId, linkId, cancellationToken), timeProvider.GetUtcNow());

  /// <summary>
  /// Creates a link with a fresh 128-bit token and returns the clear token together with the link.
  /// Only the token's hash is stored, so this response is the only place the token ever appears.
  /// </summary>
  /// <exception cref="DomainNotFoundException">No application has this id.</exception>
  /// <exception cref="DomainValidationException">A setting breaks a link rule or a document is not link-only.</exception>
  public async Task<CreatedApplicationLink> CreateAsync(long applicationId, ApplicationLinkForm form, CancellationToken cancellationToken)
  {
    await EnsureApplicationExistsAsync(applicationId, cancellationToken);
    var now = timeProvider.GetUtcNow();
    var (token, hash) = ApplicationLinkToken.Generate();
    var link = ApplicationLink.Create(applicationId, ToSettings(form, now), hash, now);
    link.AttachDocuments(await LoadDocumentsAsync(form.DocumentIds, cancellationToken));
    dbContext.ApplicationLinks.Add(link);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new CreatedApplicationLink(token, ToAdmin(link, now));
  }

  /// <summary>Replaces a link's settings and documents; the token stays the same.</summary>
  /// <exception cref="DomainNotFoundException">The application has no link with this id.</exception>
  /// <exception cref="DomainValidationException">A setting breaks a link rule or a document is not link-only.</exception>
  /// <exception cref="DomainConflictException">The link is revoked.</exception>
  public async Task UpdateAsync(long applicationId, long linkId, ApplicationLinkForm form, CancellationToken cancellationToken)
  {
    var link = await FindAsync(applicationId, linkId, cancellationToken);
    var now = timeProvider.GetUtcNow();
    link.Update(ToSettings(form, now), now);
    link.AttachDocuments(await LoadDocumentsAsync(form.DocumentIds, cancellationToken));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Revokes a link for good; revoking it again changes nothing.</summary>
  /// <exception cref="DomainNotFoundException">The application has no link with this id.</exception>
  public async Task RevokeAsync(long applicationId, long linkId, CancellationToken cancellationToken)
  {
    var link = await FindAsync(applicationId, linkId, cancellationToken);
    link.Revoke();
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task EnsureApplicationExistsAsync(long applicationId, CancellationToken cancellationToken)
  {
    if (!await dbContext.Applications.AnyAsync(application => application.Id == applicationId, cancellationToken))
    {
      throw new DomainNotFoundException($"Application {applicationId} does not exist.");
    }
  }

  private async Task<ApplicationLink> FindAsync(long applicationId, long linkId, CancellationToken cancellationToken)
    => await dbContext.ApplicationLinks
      .Include(link => link.Documents)
      .SingleOrDefaultAsync(link => link.Id == linkId && link.ApplicationId == applicationId, cancellationToken)
      ?? throw new DomainNotFoundException($"Application {applicationId} has no link {linkId}.");

  private async Task<IReadOnlyList<Documents.Domain.Document>> LoadDocumentsAsync(
    IReadOnlyList<long>? documentIds,
    CancellationToken cancellationToken)
  {
    var ids = (documentIds ?? []).Distinct().ToList();
    var documents = await dbContext.Documents.AsNoTracking()
      .Where(document => ids.Contains(document.Id))
      .ToListAsync(cancellationToken);
    var missing = ids.Except(documents.Select(document => document.Id)).ToList();
    return missing.Count == 0
      ? documents
      : throw new DomainValidationException("documentIds", $"Document(s) {string.Join(", ", missing)} do not exist.");
  }

  private static LinkSettings ToSettings(ApplicationLinkForm form, DateTimeOffset now)
    => new(form.Profile, form.GreetingDe, form.GreetingEn, form.ExpiresAt ?? now + ApplicationLink.DefaultLifetime, form.UnlockFullPdf);

  private static AdminApplicationLink ToAdmin(ApplicationLink link, DateTimeOffset now)
    => new(
      link.Id,
      link.ApplicationId,
      link.Profile,
      link.GreetingDe,
      link.GreetingEn,
      link.ExpiresAt,
      link.IsRevoked,
      link.IsActiveAt(now),
      link.UnlockFullPdf,
      link.Documents.Select(document => document.DocumentId).Order().ToList(),
      link.CreatedAt);
}
