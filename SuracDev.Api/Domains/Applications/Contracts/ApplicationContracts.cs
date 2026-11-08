using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Audience;

namespace SuracDev.Api.Domains.Applications.Contracts;

/// <summary>An application as the admin edits it, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Company">Required.</param>
/// <param name="Position">Required; the advertised job title.</param>
/// <param name="JobAdUrl">Absolute http(s) URL of the job ad, or null.</param>
/// <param name="AppliedOn">The day the application was sent; null while it is a draft.</param>
/// <param name="Status">Where the application stands; <c>Unknown</c> is rejected.</param>
/// <param name="Notes">Private Markdown notes.</param>
public sealed record ApplicationForm(
  string? Company,
  string? Position,
  string? JobAdUrl,
  DateOnly? AppliedOn,
  ApplicationStatus Status,
  string? Notes);

/// <summary>An application in the admin area.</summary>
/// <param name="Id">The application's identifier.</param>
/// <param name="Company">The employer.</param>
/// <param name="Position">The advertised job title.</param>
/// <param name="JobAdUrl">The job ad, if recorded.</param>
/// <param name="AppliedOn">The day it was sent, if it was.</param>
/// <param name="Status">Where it stands.</param>
/// <param name="Notes">Private Markdown notes, never shown outside the admin area.</param>
/// <param name="CreatedAt">When it was entered.</param>
/// <param name="UpdatedAt">When it was last saved.</param>
public sealed record AdminApplication(
  long Id,
  string Company,
  string Position,
  string? JobAdUrl,
  DateOnly? AppliedOn,
  ApplicationStatus Status,
  string? Notes,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);

/// <summary>The settings of an application link, for <c>POST</c> and <c>PUT</c>.</summary>
/// <param name="Profile">The audience the link is made for; decides the section order.</param>
/// <param name="GreetingDe">Optional one-line German greeting.</param>
/// <param name="GreetingEn">Optional one-line English greeting; the German one is used when empty.</param>
/// <param name="ExpiresAt">Must lie in the future; null sets it to 90 days from now.</param>
/// <param name="UnlockFullPdf">Whether the link delivers the full CV with phone and street address.</param>
/// <param name="DocumentIds">The link-only documents the link unlocks.</param>
public sealed record ApplicationLinkForm(
  AudienceProfile Profile,
  string? GreetingDe,
  string? GreetingEn,
  DateTimeOffset? ExpiresAt,
  bool UnlockFullPdf,
  IReadOnlyList<long> DocumentIds);

/// <summary>An application link in the admin area. The clear token is not part of it; it exists only once, at
/// creation.</summary>
/// <param name="Id">The link's identifier.</param>
/// <param name="ApplicationId">The application the link belongs to.</param>
/// <param name="Profile">The audience the link is made for.</param>
/// <param name="GreetingDe">German greeting, if any.</param>
/// <param name="GreetingEn">English greeting, if any.</param>
/// <param name="ExpiresAt">When the link stops working.</param>
/// <param name="IsRevoked">Whether the admin has revoked it.</param>
/// <param name="IsActive">Whether it opens right now: not revoked and not expired.</param>
/// <param name="UnlockFullPdf">Whether it delivers the full CV.</param>
/// <param name="DocumentIds">The documents it unlocks.</param>
/// <param name="CreatedAt">When it was created.</param>
public sealed record AdminApplicationLink(
  long Id,
  long ApplicationId,
  AudienceProfile Profile,
  string? GreetingDe,
  string? GreetingEn,
  DateTimeOffset ExpiresAt,
  bool IsRevoked,
  bool IsActive,
  bool UnlockFullPdf,
  IReadOnlyList<long> DocumentIds,
  DateTimeOffset CreatedAt);

/// <summary>A new link together with its clear token, which is shown this once and never again.</summary>
/// <param name="Token">The capability token; the link's path is <c>/b/{token}</c>.</param>
/// <param name="Link">The stored link.</param>
public sealed record CreatedApplicationLink(string Token, AdminApplicationLink Link);

/// <summary>
/// What an application link unlocks, in one locale. Content comes from the public endpoints; the
/// link only decides order, greeting and downloads (docs/decisions.md, "One site, three audience profiles").
/// </summary>
/// <param name="Greeting">The greeting in the requested locale or German, or null.</param>
/// <param name="Sections">The start-page sections in the order of the link's profile.</param>
/// <param name="FullCv">Whether <c>cv.pdf</c> of this link is the full variant with phone and street address.</param>
/// <param name="Documents">The documents the link unlocks, for <c>documents/{id}</c> of this link.</param>
public sealed record PublicApplicationLink(
  string? Greeting,
  IReadOnlyList<SiteSection> Sections,
  bool FullCv,
  IReadOnlyList<PublicLinkDocument> Documents);

/// <summary>A document unlocked by a link.</summary>
/// <param name="Id">The document's identifier within the link.</param>
/// <param name="Label">The label in the requested locale, German, or the file name when none is set.</param>
/// <param name="FileName">The file name it downloads under.</param>
/// <param name="Kind">Certificate, cover letter or other.</param>
public sealed record PublicLinkDocument(long Id, string Label, string FileName, DocumentKind Kind);

/// <summary>
/// The statistics page: the status pipeline, the response rate and the link activity per
/// application. Built from link events only, so it knows nothing about visitors.
/// </summary>
/// <param name="SentCount">Applications in any status except <c>Draft</c>.</param>
/// <param name="RespondedCount">Sent applications with a reply, interview, offer or rejection.</param>
/// <param name="ResponseRate">Responded divided by sent, from 0 to 1; null when nothing was sent.</param>
/// <param name="Pipeline">Every status in pipeline order with its count, zeros included.</param>
/// <param name="Applications">The activity per application, newest application first.</param>
public sealed record ApplicationStatistics(
  int SentCount,
  int RespondedCount,
  double? ResponseRate,
  IReadOnlyList<StatusCount> Pipeline,
  IReadOnlyList<ApplicationActivity> Applications);

/// <summary>What happened on the links of one application.</summary>
/// <param name="ApplicationId">The application's identifier.</param>
/// <param name="Company">The employer.</param>
/// <param name="Position">The advertised job title.</param>
/// <param name="Status">Where the application stands.</param>
/// <param name="AppliedOn">The day it was sent, if it was.</param>
/// <param name="LinkCount">How many links were created for it, revoked ones included.</param>
/// <param name="Opens">How often its links were opened.</param>
/// <param name="PdfDownloads">How often the CV was downloaded through them.</param>
/// <param name="DocumentDownloads">How often attached documents were downloaded.</param>
/// <param name="FirstOpenedAt">The first opening of any of its links, or null.</param>
/// <param name="MinutesToFirstOpen">Whole minutes from the first link's creation to the first opening, or null.</param>
public sealed record ApplicationActivity(
  long ApplicationId,
  string Company,
  string Position,
  ApplicationStatus Status,
  DateOnly? AppliedOn,
  int LinkCount,
  int Opens,
  int PdfDownloads,
  int DocumentDownloads,
  DateTimeOffset? FirstOpenedAt,
  int? MinutesToFirstOpen);
