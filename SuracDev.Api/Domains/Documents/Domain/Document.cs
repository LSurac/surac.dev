using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Documents.Domain;

/// <summary>What a document is. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum DocumentKind
{
  Unknown = 0,
  Photo = 1,
  Certificate = 2,
  CoverLetter = 3,
  Image = 4,
  Other = 5,
}

/// <summary>Who may download a document. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum DocumentVisibility
{
  Unknown = 0,
  Public = 1,
  LinkOnly = 2,
}

/// <summary>
/// The facts of a stored upload. <paramref name="ContentType"/> is the sniffed type, never the one the
/// browser claimed; <paramref name="StoragePath"/> is the random path below the documents volume.
/// </summary>
public sealed record DocumentUpload(
  string? FileName,
  string ContentType,
  long SizeBytes,
  string StoragePath,
  string Sha256,
  DocumentKind Kind,
  DocumentVisibility Visibility,
  string? LabelDe,
  string? LabelEn);

/// <summary>
/// An uploaded file: photo, case-study image, certificate or cover letter. Certificates and cover
/// letters carry employer names and signatures and are therefore never public
/// (docs/security-and-privacy.md, "Personal data inventory").
/// </summary>
public sealed class Document
{
  public const long MaxSizeBytes = 10 * 1024 * 1024;
  public const int MaxFileNameLength = 200;
  public const int MaxStoragePathLength = 200;
  public const int Sha256HexLength = 64;

  private readonly List<DocumentTranslation> translations = [];

  private Document()
  {
  }

  public long Id { get; private set; }

  /// <summary>The original file name without any client path; used for <c>Content-Disposition</c>.</summary>
  public string FileName { get; private set; } = null!;

  public string ContentType { get; private set; } = null!;

  public long SizeBytes { get; private set; }

  /// <summary>Random relative path below the documents volume, outside any web root.</summary>
  public string StoragePath { get; private set; } = null!;

  /// <summary>Lowercase hex SHA-256 of the content.</summary>
  public string Sha256 { get; private set; } = null!;

  public DocumentKind Kind { get; private set; }

  public DocumentVisibility Visibility { get; private set; }

  public DateTimeOffset UploadedAt { get; private set; }

  /// <summary>True when the open site may serve the document without an application link.</summary>
  public bool IsPublic => Visibility == DocumentVisibility.Public;

  public IReadOnlyList<DocumentTranslation> Translations => translations;

  /// <summary>Records an upload after checking size, type, and the kind/visibility rules.</summary>
  public static Document Create(DocumentUpload upload, DateTimeOffset now)
  {
    if (upload.SizeBytes is <= 0 or > MaxSizeBytes)
    {
      throw new DomainValidationException("file", $"The file must be between 1 byte and {MaxSizeBytes / 1024 / 1024} MB.");
    }

    if (!DocumentContentTypes.All.Contains(upload.ContentType))
    {
      throw new DomainValidationException("file", "Only PDF, PNG, JPEG and WebP files are allowed.");
    }

    var kind = Require.Known(upload.Kind, "kind");
    if (kind is DocumentKind.Photo or DocumentKind.Image && !DocumentContentTypes.IsImage(upload.ContentType))
    {
      throw new DomainValidationException("file", "Photos and images must be PNG, JPEG or WebP.");
    }

    var document = new Document
    {
      FileName = Require.Text(BareFileName(upload.FileName), "fileName", MaxFileNameLength),
      ContentType = upload.ContentType,
      SizeBytes = upload.SizeBytes,
      StoragePath = upload.StoragePath,
      Sha256 = upload.Sha256,
      Kind = kind,
      UploadedAt = now,
    };
    document.ChangeVisibility(upload.Visibility);
    document.Relabel(upload.LabelDe, upload.LabelEn);
    return document;
  }

  /// <summary>Switches between public and link-only; certificates and cover letters stay link-only.</summary>
  public void ChangeVisibility(DocumentVisibility visibility)
  {
    var known = Require.Known(visibility, "visibility");
    if (known == DocumentVisibility.Public && Kind is DocumentKind.Certificate or DocumentKind.CoverLetter)
    {
      throw new DomainValidationException("visibility", "Certificates and cover letters are never public.");
    }

    Visibility = known;
  }

  /// <summary>Sets both labels; the German row always exists, an empty English label removes the English one.</summary>
  public void Relabel(string? labelDe, string? labelEn)
  {
    SetLabel(Locale.German, labelDe);
    SetLabel(Locale.English, labelEn);
  }

  private void SetLabel(Locale locale, string? label)
    => TranslationList.Set(
      translations,
      locale,
      string.IsNullOrWhiteSpace(label),
      () => DocumentTranslation.Create(locale, label),
      translation => translation.Apply(label));

  private static string? BareFileName(string? fileName)
    => fileName?[(fileName.LastIndexOfAny(['/', '\\']) + 1)..];
}

/// <summary>The label of a document in one locale; also the alt text of an image.</summary>
public sealed class DocumentTranslation : ITranslation
{
  private DocumentTranslation()
  {
  }

  public long Id { get; private set; }

  public long DocumentId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Label { get; private set; }

  internal static DocumentTranslation Create(Locale locale, string? label)
  {
    var translation = new DocumentTranslation { Locale = locale };
    translation.Apply(label);
    return translation;
  }

  internal void Apply(string? label)
    => Label = Require.Translated(label, Locale, "label", TextLimits.Name);
}
