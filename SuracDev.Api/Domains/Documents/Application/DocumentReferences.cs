using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Documents.Application;

/// <summary>
/// Checks documents that other content points to before it is saved, so a broken reference is a
/// precise 400 for the admin instead of a foreign-key violation.
/// </summary>
public sealed class DocumentReferences(SuracDevDbContext dbContext)
{
  /// <summary>
  /// Ensures that every given id names a public image: the open site shows it to everyone, so a
  /// link-only document or a PDF there would either leak or break.
  /// </summary>
  /// <exception cref="DomainValidationException">An id is unknown, not public or not an image.</exception>
  public async Task EnsurePublicImagesAsync(IReadOnlyCollection<long> documentIds, string field, CancellationToken cancellationToken)
  {
    if (documentIds.Count == 0)
    {
      return;
    }

    var found = await dbContext.Documents.AsNoTracking()
      .Where(document => documentIds.Contains(document.Id))
      .Select(document => new { document.Id, document.Visibility, document.ContentType })
      .ToListAsync(cancellationToken);

    var usable = found
      .Where(document => document.Visibility == DocumentVisibility.Public && DocumentContentTypes.IsImage(document.ContentType))
      .Select(document => document.Id)
      .ToHashSet();
    var unusable = documentIds.Where(id => !usable.Contains(id)).ToList();
    if (unusable.Count > 0)
    {
      throw new DomainValidationException(
        field, $"Document(s) {string.Join(", ", unusable)} do not exist or are not public images.");
    }
  }
}
