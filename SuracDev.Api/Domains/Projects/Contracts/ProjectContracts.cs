using SuracDev.Api.Domains.Projects.Domain;

namespace SuracDev.Api.Domains.Projects.Contracts;

/// <summary>A project card of the open site, in one locale.</summary>
/// <param name="Id">Stable identifier, for list keys.</param>
/// <param name="Slug">URL segment of the case study (<c>/projekte/{slug}</c>, <c>/en/projects/{slug}</c>).</param>
/// <param name="Status">Where the project stands.</param>
/// <param name="StartedYear">Year the project started, if known.</param>
/// <param name="EndedYear">Year it ended, or null while it runs.</param>
/// <param name="LiveUrl">The running product, if public.</param>
/// <param name="RepositoryUrl">The source code, if public.</param>
/// <param name="StackTags">Technology names in display order.</param>
/// <param name="IsFeatured">Shown on the start page.</param>
/// <param name="Title">Project title.</param>
/// <param name="Teaser">One or two sentences for the card.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for
/// them.</param>
public sealed record PublicProjectCard(
  long Id,
  string Slug,
  ProjectStatus Status,
  int? StartedYear,
  int? EndedYear,
  string? LiveUrl,
  string? RepositoryUrl,
  IReadOnlyList<string> StackTags,
  bool IsFeatured,
  string Title,
  string Teaser,
  IReadOnlyList<string> FallbackFields);

/// <summary>The case study of a project, in one locale.</summary>
/// <param name="Slug">URL segment, shared by both locales.</param>
/// <param name="Status">Where the project stands.</param>
/// <param name="StartedYear">Year the project started, if known.</param>
/// <param name="EndedYear">Year it ended, or null while it runs.</param>
/// <param name="LiveUrl">The running product, if public.</param>
/// <param name="RepositoryUrl">The source code, if public.</param>
/// <param name="StackTags">Technology names in display order.</param>
/// <param name="Title">Project title.</param>
/// <param name="Teaser">The card text, used as lede and meta description.</param>
/// <param name="Problem">What had to be solved.</param>
/// <param name="MyRole">The maintainer's own share.</param>
/// <param name="Outcome">What came out of it.</param>
/// <param name="CaseStudyHtml">The case study rendered from the restricted Markdown subset; safe to bind as inner
/// HTML.</param>
/// <param name="Decisions">Architectural choices with their trade-offs, in order.</param>
/// <param name="Images">Screenshots and diagrams, in order.</param>
/// <param name="FallbackFields">JSON names of the fields shown in German because the requested locale has no text for
/// them.</param>
public sealed record PublicCaseStudy(
  string Slug,
  ProjectStatus Status,
  int? StartedYear,
  int? EndedYear,
  string? LiveUrl,
  string? RepositoryUrl,
  IReadOnlyList<string> StackTags,
  string Title,
  string Teaser,
  string? Problem,
  string? MyRole,
  string? Outcome,
  string CaseStudyHtml,
  IReadOnlyList<PublicDecision> Decisions,
  IReadOnlyList<PublicImage> Images,
  IReadOnlyList<string> FallbackFields);

/// <summary>One decision of a case study.</summary>
/// <param name="Title">What was decided.</param>
/// <param name="TradeOff">What it cost and why it was worth it.</param>
public sealed record PublicDecision(string Title, string TradeOff);

/// <summary>One image of a case study.</summary>
/// <param name="DocumentId">Served by <c>GET /api/public/documents/{id}</c>.</param>
/// <param name="Caption">Caption and alt text, if given.</param>
public sealed record PublicImage(long DocumentId, string? Caption);

/// <summary>A project for the admin editor, with both locales, decisions and images.</summary>
/// <param name="Id">The project's identifier.</param>
/// <param name="Slug">Unique URL segment.</param>
/// <param name="Status">Where the project stands; never <c>Unknown</c>.</param>
/// <param name="StartedYear">Optional start year.</param>
/// <param name="EndedYear">Optional end year, not before the start year.</param>
/// <param name="LiveUrl">Optional absolute http(s) URL.</param>
/// <param name="RepositoryUrl">Optional absolute http(s) URL.</param>
/// <param name="StackTags">At most 20 technology names.</param>
/// <param name="SortOrder">Position among the projects.</param>
/// <param name="IsFeatured">Shown on the start page; at most five visible projects.</param>
/// <param name="IsVisible">Hidden projects never reach the open site.</param>
/// <param name="De">The German texts; title and teaser are required.</param>
/// <param name="En">The English texts, or null when there are none.</param>
/// <param name="Decisions">At most five decisions in order.</param>
/// <param name="Images">At most twelve images in order.</param>
public sealed record AdminProject(
  long Id,
  string Slug,
  ProjectStatus Status,
  int? StartedYear,
  int? EndedYear,
  string? LiveUrl,
  string? RepositoryUrl,
  IReadOnlyList<string> StackTags,
  int SortOrder,
  bool IsFeatured,
  bool IsVisible,
  ProjectTextsForm De,
  ProjectTextsForm? En,
  IReadOnlyList<DecisionForm> Decisions,
  IReadOnlyList<ImageForm> Images);

/// <summary>The writable fields of a project, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Slug">Unique URL segment: lowercase letters and digits joined by single hyphens.</param>
/// <param name="Status">Where the project stands; never <c>Unknown</c>.</param>
/// <param name="StartedYear">Optional start year.</param>
/// <param name="EndedYear">Optional end year, not before the start year.</param>
/// <param name="LiveUrl">Optional absolute http(s) URL.</param>
/// <param name="RepositoryUrl">Optional absolute http(s) URL.</param>
/// <param name="StackTags">At most 20 technology names; blanks are dropped.</param>
/// <param name="SortOrder">Position among the projects.</param>
/// <param name="IsFeatured">Shown on the start page; at most five visible projects.</param>
/// <param name="IsVisible">Whether the open site shows the project.</param>
/// <param name="De">The German texts; title and teaser are required.</param>
/// <param name="En">The English texts, or null to remove them.</param>
/// <param name="Decisions">At most five decisions in order; replaces the existing ones.</param>
/// <param name="Images">At most twelve public image documents in order; replaces the existing ones.</param>
public sealed record ProjectForm(
  string Slug,
  ProjectStatus Status,
  int? StartedYear,
  int? EndedYear,
  string? LiveUrl,
  string? RepositoryUrl,
  IReadOnlyList<string> StackTags,
  int SortOrder,
  bool IsFeatured,
  bool IsVisible,
  ProjectTextsForm De,
  ProjectTextsForm? En,
  IReadOnlyList<DecisionForm> Decisions,
  IReadOnlyList<ImageForm> Images);

/// <summary>The translatable texts of a project in one locale.</summary>
/// <param name="Title">Required in German.</param>
/// <param name="Teaser">Required in German.</param>
/// <param name="Problem">Optional paragraph.</param>
/// <param name="MyRole">Optional paragraph.</param>
/// <param name="Outcome">Optional paragraph.</param>
/// <param name="CaseStudyMarkdown">Optional Markdown in the restricted subset.</param>
public sealed record ProjectTextsForm(
  string? Title,
  string? Teaser,
  string? Problem,
  string? MyRole,
  string? Outcome,
  string? CaseStudyMarkdown);

/// <summary>One decision with both locales.</summary>
/// <param name="De">German title and trade-off, both required.</param>
/// <param name="En">English texts, or null to fall back to German.</param>
public sealed record DecisionForm(DecisionTextsForm De, DecisionTextsForm? En);

/// <summary>The texts of a decision in one locale.</summary>
public sealed record DecisionTextsForm(string? Title, string? TradeOff);

/// <summary>One image of a case study with both captions.</summary>
/// <param name="DocumentId">A public image document.</param>
/// <param name="CaptionDe">Optional German caption.</param>
/// <param name="CaptionEn">Optional English caption.</param>
public sealed record ImageForm(long DocumentId, string? CaptionDe, string? CaptionEn);
