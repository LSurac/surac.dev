using SuracDev.Api.Domains.Interests.Domain;

namespace SuracDev.Api.Domains.Interests.Contracts;

/// <summary>A section of the "Über mich" page as the open site shows it, in one locale.</summary>
/// <param name="Id">The interest's identifier.</param>
/// <param name="Kind">The topic; the site may style sections by it, never hide them.</param>
/// <param name="Title">The heading, in the requested locale or German.</param>
/// <param name="BodyHtml">The body rendered from the restricted Markdown, or null when there is none.</param>
/// <param name="ImageDocumentId">A public image, served by <c>GET /api/public/documents/{id}</c>; null when none is set.</param>
/// <param name="ImageAlt">The image's alternative text: its document label, or the title when the image has no label.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for them.</param>
public sealed record PublicInterest(
  long Id,
  InterestKind Kind,
  string Title,
  string? BodyHtml,
  long? ImageDocumentId,
  string? ImageAlt,
  IReadOnlyList<string> FallbackFields);

/// <summary>An interest for the admin editor, both locales side by side. Read with <c>GET</c>, written with <c>PUT</c>.</summary>
/// <param name="Id">The interest's identifier.</param>
/// <param name="Kind">The topic; <c>Unknown</c> is rejected.</param>
/// <param name="SortOrder">Position on the page, ascending.</param>
/// <param name="IsVisible">Whether the open site shows it.</param>
/// <param name="ImageDocumentId">A public image document, or null.</param>
/// <param name="De">The German texts; the title is required.</param>
/// <param name="En">The English texts, or null; empty fields fall back to German.</param>
public sealed record AdminInterest(
  long Id,
  InterestKind Kind,
  int SortOrder,
  bool IsVisible,
  long? ImageDocumentId,
  InterestTextsForm De,
  InterestTextsForm? En);

/// <summary>An interest as the admin saves it, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Kind">The topic; <c>Unknown</c> is rejected.</param>
/// <param name="SortOrder">Position on the page, ascending.</param>
/// <param name="IsVisible">Whether the open site shows it.</param>
/// <param name="ImageDocumentId">A public image document, or null for none.</param>
/// <param name="De">The German texts; the title is required.</param>
/// <param name="En">The English texts, or null to remove them.</param>
public sealed record InterestForm(
  InterestKind Kind,
  int SortOrder,
  bool IsVisible,
  long? ImageDocumentId,
  InterestTextsForm De,
  InterestTextsForm? En);

/// <summary>The texts of an interest in one locale.</summary>
/// <param name="Title">The heading.</param>
/// <param name="Body">Markdown: headings, lists, links, emphasis.</param>
public sealed record InterestTextsForm(string? Title, string? Body);
