namespace SuracDev.Api.Domains.Documents.Domain;

/// <summary>
/// Detects the content type of an upload from its first bytes. The extension and the type the
/// browser claims are never trusted (docs/security-and-privacy.md, "Uploads").
/// </summary>
public static class FileSignature
{
  /// <summary>How many leading bytes <see cref="Detect"/> needs to recognise every allowed type.</summary>
  public const int HeaderLength = 12;

  private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
  private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
  private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
  private static readonly byte[] Riff = "RIFF"u8.ToArray();
  private static readonly byte[] WebP = "WEBP"u8.ToArray();

  /// <summary>The content type of an allowed format (PDF, PNG, JPEG, WebP), or <c>null</c> for anything else.</summary>
  public static string? Detect(ReadOnlySpan<byte> header)
  {
    if (header.StartsWith(Pdf))
    {
      return DocumentContentTypes.Pdf;
    }

    if (header.StartsWith(Png))
    {
      return DocumentContentTypes.Png;
    }

    if (header.StartsWith(Jpeg))
    {
      return DocumentContentTypes.Jpeg;
    }

    var isWebP = header.Length >= HeaderLength && header.StartsWith(Riff) && header[8..12].SequenceEqual(WebP);
    return isWebP ? DocumentContentTypes.WebP : null;
  }
}

/// <summary>The content types a document may have.</summary>
public static class DocumentContentTypes
{
  public const string Pdf = "application/pdf";
  public const string Png = "image/png";
  public const string Jpeg = "image/jpeg";
  public const string WebP = "image/webp";

  /// <summary>Every allowed type.</summary>
  public static IReadOnlySet<string> All { get; } = new HashSet<string> { Pdf, Png, Jpeg, WebP };

  /// <summary>True for the image types, which photos and case-study images must have.</summary>
  public static bool IsImage(string contentType) => contentType is Png or Jpeg or WebP;
}
