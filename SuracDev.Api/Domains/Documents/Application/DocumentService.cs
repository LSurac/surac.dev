using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Contracts;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Documents.Application;

/// <summary>A stored file opened for delivery, with what the response headers need.</summary>
public sealed record DocumentContent(
  Stream Content,
  string ContentType,
  string FileName,
  DateTimeOffset UploadedAt,
  string Sha256);

/// <summary>
/// The use cases of documents: upload, list, relabel, change visibility, delete and deliver. Keeps
/// the database row and the file on the volume in step, and refuses changes that would break or
/// leak what the open site or an application link shows.
/// </summary>
public sealed class DocumentService(SuracDevDbContext dbContext, DocumentStorage storage, TimeProvider timeProvider)
{
  /// <summary>Every document, newest first.</summary>
  public async Task<IReadOnlyList<AdminDocument>> ListAsync(CancellationToken cancellationToken)
  {
    var documents = await dbContext.Documents.AsNoTracking()
      .Include(document => document.Translations)
      .OrderByDescending(document => document.UploadedAt)
      .ThenByDescending(document => document.Id)
      .ToListAsync(cancellationToken);
    return documents.Select(ToAdmin).ToList();
  }

  /// <summary>
  /// Stores an upload: the type is detected from the first bytes, the file is written under a
  /// random name and hashed, then the row is saved. A rejected upload leaves no file behind.
  /// </summary>
  /// <exception cref="DomainValidationException">Empty, too large, of a type not allowed, or breaking a kind/visibility rule.</exception>
  public async Task<AdminDocument> UploadAsync(
    IFormFile file,
    DocumentKind kind,
    DocumentVisibility visibility,
    string? labelDe,
    string? labelEn,
    CancellationToken cancellationToken)
  {
    if (file.Length is <= 0 or > Document.MaxSizeBytes)
    {
      throw new DomainValidationException("file", $"The file must be between 1 byte and {Document.MaxSizeBytes / 1024 / 1024} MB.");
    }

    await using var stream = file.OpenReadStream();
    var contentType = await DetectContentTypeAsync(stream, cancellationToken)
      ?? throw new DomainValidationException("file", "Only PDF, PNG, JPEG and WebP files are allowed.");

    var stored = await storage.SaveAsync(stream, cancellationToken);
    try
    {
      var document = Document.Create(
        new DocumentUpload(file.FileName, contentType, stored.SizeBytes, stored.RelativePath, stored.Sha256, kind, visibility, labelDe, labelEn),
        timeProvider.GetUtcNow());
      dbContext.Documents.Add(document);
      await dbContext.SaveChangesAsync(cancellationToken);
      return ToAdmin(document);
    }
    catch
    {
      // The row was not saved; the file must not stay behind as an orphan.
      storage.Delete(stored.RelativePath);
      throw;
    }
  }

  /// <summary>Changes labels and visibility. A document shown on the open site stays public.</summary>
  /// <exception cref="DomainNotFoundException">No document has this id.</exception>
  /// <exception cref="DomainValidationException">The visibility breaks a kind rule.</exception>
  /// <exception cref="DomainConflictException">The document is in use on the open site and would become link-only.</exception>
  public async Task UpdateAsync(long id, DocumentUpdateForm form, CancellationToken cancellationToken)
  {
    var document = await FindAsync(id, cancellationToken);
    if (form.Visibility != DocumentVisibility.Public && document.IsPublic && await IsShownPubliclyAsync(id, cancellationToken))
    {
      throw new DomainConflictException("The document is shown on the open site (profile photo, project or interest image).");
    }

    document.ChangeVisibility(form.Visibility);
    document.Relabel(form.LabelDe, form.LabelEn);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes the row and then the file.</summary>
  /// <exception cref="DomainNotFoundException">No document has this id.</exception>
  /// <exception cref="DomainConflictException">The document is still referenced by the profile, a project, an interest or a link.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var document = await FindAsync(id, cancellationToken);
    var attachedToLink = await dbContext.ApplicationLinks
      .SelectMany(link => link.Documents)
      .AnyAsync(attached => attached.DocumentId == id, cancellationToken);
    if (attachedToLink || await IsShownPubliclyAsync(id, cancellationToken))
    {
      throw new DomainConflictException("The document is still in use; remove it from the profile, projects, interests and links first.");
    }

    dbContext.Documents.Remove(document);
    await dbContext.SaveChangesAsync(cancellationToken);
    storage.Delete(document.StoragePath);
  }

  /// <summary>Opens a public document for the open site.</summary>
  /// <exception cref="DomainNotFoundException">No public document has this id; link-only ones are indistinguishable from missing ones.</exception>
  public async Task<DocumentContent> OpenPublicAsync(long id, CancellationToken cancellationToken)
  {
    var document = await dbContext.Documents.AsNoTracking()
      .SingleOrDefaultAsync(d => d.Id == id && d.Visibility == DocumentVisibility.Public, cancellationToken)
      ?? throw new DomainNotFoundException($"Document {id} does not exist.");
    return Open(document);
  }

  /// <summary>Opens any document for the admin.</summary>
  /// <exception cref="DomainNotFoundException">No document has this id.</exception>
  public async Task<DocumentContent> OpenAdminAsync(long id, CancellationToken cancellationToken)
    => Open(await FindAsync(id, cancellationToken));

  private DocumentContent Open(Document document)
    => new(storage.OpenRead(document.StoragePath), document.ContentType, document.FileName, document.UploadedAt, document.Sha256);

  private async Task<Document> FindAsync(long id, CancellationToken cancellationToken)
    => await dbContext.Documents.Include(document => document.Translations)
      .SingleOrDefaultAsync(document => document.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Document {id} does not exist.");

  private async Task<bool> IsShownPubliclyAsync(long id, CancellationToken cancellationToken)
    => await dbContext.Profiles.AnyAsync(profile => profile.PhotoDocumentId == id, cancellationToken)
      || await dbContext.Projects.SelectMany(project => project.Images).AnyAsync(image => image.DocumentId == id, cancellationToken)
      || await dbContext.Interests.AnyAsync(interest => interest.ImageDocumentId == id, cancellationToken);

  private static async Task<string?> DetectContentTypeAsync(Stream stream, CancellationToken cancellationToken)
  {
    var header = new byte[FileSignature.HeaderLength];
    var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
    stream.Position = 0;
    return FileSignature.Detect(header.AsSpan(0, read));
  }

  private static AdminDocument ToAdmin(Document document)
    => new(
      document.Id,
      document.FileName,
      document.ContentType,
      document.SizeBytes,
      document.Kind,
      document.Visibility,
      document.UploadedAt,
      document.Translations.Single(t => t.Locale == Locale.German).Label,
      document.Translations.SingleOrDefault(t => t.Locale == Locale.English)?.Label);
}
