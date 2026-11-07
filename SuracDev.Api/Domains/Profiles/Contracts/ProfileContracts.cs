namespace SuracDev.Api.Domains.Profiles.Contracts;

/// <summary>
/// The profile as the open site shows it, in one locale. Phone and street address are never part
/// of it (docs/content-model.md, "Visibility rules").
/// </summary>
/// <param name="FullName">The person's name.</param>
/// <param name="Email">The public contact address.</param>
/// <param name="Location">City or region, never a street address.</param>
/// <param name="GitHubUrl">Link to the GitHub profile, if any.</param>
/// <param name="LinkedInUrl">Link to the LinkedIn profile, if any.</param>
/// <param name="PhotoDocumentId">The hero photo, served by <c>GET /api/public/documents/{id}</c>; null when none is set.</param>
/// <param name="TitleLine">The search-term line under the name ("Fullstack-Entwickler · .NET &amp; Angular").</param>
/// <param name="Headline">The hero sentence.</param>
/// <param name="Intro">The short profile, three to five sentences.</param>
/// <param name="AvailabilityLine">An optional availability note.</param>
/// <param name="Facts">The fact bar, in display order, at most four cells.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for them.</param>
public sealed record PublicProfile(
  string FullName,
  string Email,
  string Location,
  string? GitHubUrl,
  string? LinkedInUrl,
  long? PhotoDocumentId,
  string TitleLine,
  string Headline,
  string? Intro,
  string? AvailabilityLine,
  IReadOnlyList<PublicFact> Facts,
  IReadOnlyList<string> FallbackFields);

/// <summary>One cell of the fact bar.</summary>
/// <param name="Value">The number or short value ("5", "1,8", "C1").</param>
/// <param name="Label">What the value means, in the requested locale or German.</param>
public sealed record PublicFact(string Value, string Label);

/// <summary>
/// The complete profile for the admin editor, both locales side by side. The same shape is read
/// with <c>GET</c> and written back with <c>PUT</c>.
/// </summary>
/// <param name="FullName">Required.</param>
/// <param name="Email">Required; a single address without display name.</param>
/// <param name="Phone">Only printed in the full CV PDF.</param>
/// <param name="Location">Required; city or region.</param>
/// <param name="AddressLines">Street address for the full PDF and the Impressum, at most four lines.</param>
/// <param name="GitHubUrl">Absolute http(s) URL or null.</param>
/// <param name="LinkedInUrl">Absolute http(s) URL or null.</param>
/// <param name="PhotoDocumentId">A public image document, or null for no photo.</param>
/// <param name="De">The German texts; title line and headline are required.</param>
/// <param name="En">The English texts, or null when there are none yet; empty fields fall back to German.</param>
/// <param name="Facts">The fact bar in display order, at most four cells.</param>
public sealed record AdminProfile(
  string FullName,
  string Email,
  string? Phone,
  string Location,
  IReadOnlyList<string> AddressLines,
  string? GitHubUrl,
  string? LinkedInUrl,
  long? PhotoDocumentId,
  ProfileTextsForm De,
  ProfileTextsForm? En,
  IReadOnlyList<FactForm> Facts);

/// <summary>The translatable texts of the profile in one locale.</summary>
public sealed record ProfileTextsForm(string? TitleLine, string? Headline, string? Intro, string? AvailabilityLine);

/// <summary>One cell of the fact bar with both labels.</summary>
/// <param name="Value">Required, at most 20 characters.</param>
/// <param name="LabelDe">Required German label.</param>
/// <param name="LabelEn">Optional English label; falls back to German.</param>
public sealed record FactForm(string Value, string LabelDe, string? LabelEn);
