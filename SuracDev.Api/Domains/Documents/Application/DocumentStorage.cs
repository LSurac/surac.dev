using System.Security.Cryptography;

namespace SuracDev.Api.Domains.Documents.Application;

/// <summary>What <see cref="DocumentStorage.SaveAsync"/> learned about a stored file.</summary>
/// <param name="RelativePath">Random path below the storage root, as stored on the document.</param>
/// <param name="SizeBytes">Bytes written.</param>
/// <param name="Sha256">Lowercase hex SHA-256 of the content.</param>
public sealed record StoredFile(string RelativePath, long SizeBytes, string Sha256);

/// <summary>
/// The files behind documents, on the documents volume outside any web root
/// (docs/security-and-privacy.md, "Uploads"). Names are random; a path from the database is
/// resolved only below the root, so a stored path can never point elsewhere.
/// </summary>
public sealed class DocumentStorage(IConfiguration configuration)
{
  private string Root
    => configuration["Documents:StoragePath"] is { Length: > 0 } path
      ? Path.GetFullPath(path)
      : throw new InvalidOperationException("Documents:StoragePath is not configured.");

  /// <summary>Writes <paramref name="content"/> under a new random name, hashing it on the way.</summary>
  public async Task<StoredFile> SaveAsync(Stream content, CancellationToken cancellationToken)
  {
    var name = Guid.NewGuid().ToString("N");
    var relativePath = $"{name[..2]}/{name}";
    var fullPath = Resolve(relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

    using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    var buffer = new byte[81_920];
    long size = 0;
    int read;
    while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
    {
      sha256.AppendData(buffer, 0, read);
      await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
      size += read;
    }

    return new StoredFile(relativePath, size, Convert.ToHexStringLower(sha256.GetHashAndReset()));
  }

  /// <summary>Opens a stored file for reading.</summary>
  /// <exception cref="FileNotFoundException">The file is missing on the volume.</exception>
  public Stream OpenRead(string relativePath)
    => new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, useAsync: true);

  /// <summary>Removes a stored file; a missing file is not an error.</summary>
  public void Delete(string relativePath) => File.Delete(Resolve(relativePath));

  private string Resolve(string relativePath)
  {
    var root = Root;
    var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
    return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
      ? fullPath
      : throw new InvalidOperationException("A document path left the storage root.");
  }
}
