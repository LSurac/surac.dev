using SuracDev.Api.Domains.Legal.Domain;

namespace SuracDev.Api.Domains.Legal.Contracts;

/// <summary>A legal page of the open site, in one locale.</summary>
/// <param name="Key">Which page.</param>
/// <param name="Title">The page title.</param>
/// <param name="BodyHtml">The body rendered from the restricted Markdown subset; safe to bind as inner HTML.</param>
/// <param name="UpdatedAt">When the shown locale last changed ("Stand").</param>
/// <param name="Contact">The provider identification from the profile; only on the Impressum (§ 5 DDG).</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for
/// them.</param>
public sealed record PublicLegalPage(
  LegalPageKey Key,
  string Title,
  string BodyHtml,
  DateTimeOffset UpdatedAt,
  ImprintContact? Contact,
  IReadOnlyList<string> FallbackFields);

/// <summary>Name, postal address and e-mail as the Impressum must show them.</summary>
/// <param name="FullName">The provider's name.</param>
/// <param name="AddressLines">The postal address, line by line.</param>
/// <param name="Email">The contact address.</param>
public sealed record ImprintContact(string FullName, IReadOnlyList<string> AddressLines, string Email);

/// <summary>A legal page for the admin editor, with both locales and their stamps.</summary>
/// <param name="Key">Which page.</param>
/// <param name="De">The German texts.</param>
/// <param name="En">The English texts, or null when there are none.</param>
public sealed record AdminLegalPage(LegalPageKey Key, AdminLegalTexts De, AdminLegalTexts? En);

/// <summary>The stored texts of a legal page in one locale.</summary>
/// <param name="Title">The page title.</param>
/// <param name="BodyMarkdown">The body in the restricted Markdown subset.</param>
/// <param name="UpdatedAt">When this locale last changed; moves only on a real change.</param>
public sealed record AdminLegalTexts(string Title, string BodyMarkdown, DateTimeOffset UpdatedAt);

/// <summary>The writable texts of a legal page, for <c>PUT</c>.</summary>
/// <param name="De">The German texts; title and body are required.</param>
/// <param name="En">The English texts, or null to remove them.</param>
public sealed record LegalPageForm(LegalTextsForm De, LegalTextsForm? En);

/// <summary>The texts of a legal page in one locale.</summary>
/// <param name="Title">Required in German.</param>
/// <param name="BodyMarkdown">Required in German; restricted Markdown subset.</param>
public sealed record LegalTextsForm(string? Title, string? BodyMarkdown);
