using SuracDev.Api.Domains.Documents.Domain;

namespace SuracDev.Api.Domains.Documents.Contracts;

/// <summary>An uploaded document as the admin sees it.</summary>
/// <param name="Id">The document's identifier; public images are served at <c>/api/public/documents/{id}</c>.</param>
/// <param name="FileName">The original file name without any client path.</param>
/// <param name="ContentType">The type detected from the file's bytes.</param>
/// <param name="SizeBytes">File size.</param>
/// <param name="Kind">What the document is.</param>
/// <param name="Visibility">Public (open site) or link-only (application links only).</param>
/// <param name="UploadedAt">When it was uploaded.</param>
/// <param name="LabelDe">German label, also the alt text of an image.</param>
/// <param name="LabelEn">English label, if any.</param>
public sealed record AdminDocument(
  long Id,
  string FileName,
  string ContentType,
  long SizeBytes,
  DocumentKind Kind,
  DocumentVisibility Visibility,
  DateTimeOffset UploadedAt,
  string? LabelDe,
  string? LabelEn);

/// <summary>The changeable fields of a document, for <c>PUT</c>. The file itself never changes; upload a new
/// one.</summary>
/// <param name="Visibility">Certificates and cover letters stay link-only; documents in use on the open site stay
/// public.</param>
/// <param name="LabelDe">Optional German label.</param>
/// <param name="LabelEn">Optional English label.</param>
public sealed record DocumentUpdateForm(DocumentVisibility Visibility, string? LabelDe, string? LabelEn);
