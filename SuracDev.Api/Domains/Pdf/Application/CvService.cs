using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Career.Application;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Domains.Pdf.Domain;
using SuracDev.Api.Domains.Pdf.Rendering;
using SuracDev.Api.Domains.Profiles.Application;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Domains.Projects.Application;
using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Domains.Skills.Application;
using SuracDev.Api.Domains.Skills.Contracts;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Pdf.Application;

/// <summary>A rendered CV with the file name it is downloaded under.</summary>
public sealed record CvFile(string FileName, byte[] Content);

/// <summary>The contact data only the <c>Full</c> variant prints (docs/content-model.md, "Visibility rules").</summary>
/// <param name="Phone">The phone number, or null when none is maintained.</param>
/// <param name="AddressLines">The street address lines; empty when none is maintained.</param>
public sealed record CvPrivateContact(string? Phone, IReadOnlyList<string> AddressLines);

/// <summary>
/// Renders the CV variants on demand. Both read exactly what the open site shows, through the same
/// public read services, so neither can contain hidden entries; only the <c>Full</c> variant adds
/// phone and street address (docs/content-model.md, "Visibility rules").
/// </summary>
public sealed class CvService(
  SuracDevDbContext dbContext,
  ProfileService profileService,
  ExperienceService experienceService,
  EducationService educationService,
  SkillService skillService,
  ProjectService projectService)
{
  /// <summary>The public CV in <paramref name="locale"/>, with German fallback like the site.</summary>
  public Task<CvFile> RenderPublicAsync(Locale locale, CancellationToken cancellationToken)
    => RenderAsync(locale, privateContact: null, cancellationToken);

  /// <summary>
  /// The full CV in <paramref name="locale"/>: the public one plus phone and street address. Only an
  /// application link with <c>UnlockFullPdf</c> may deliver it.
  /// </summary>
  public async Task<CvFile> RenderFullAsync(Locale locale, CancellationToken cancellationToken)
  {
    var privateContact = await dbContext.Profiles.AsNoTracking()
      .OrderBy(profile => profile.Id)
      .Select(profile => new CvPrivateContact(profile.Phone, profile.AddressLines))
      .FirstOrDefaultAsync(cancellationToken)
      ?? throw new DomainNotFoundException("No profile exists.");
    return await RenderAsync(locale, privateContact, cancellationToken);
  }

  private async Task<CvFile> RenderAsync(
    Locale locale,
    CvPrivateContact? privateContact,
    CancellationToken cancellationToken)
  {
    var profile = await profileService.GetPublicAsync(locale, cancellationToken);
    var experiences = await experienceService.ListPublicAsync(locale, cancellationToken);
    var education = await educationService.ListPublicAsync(locale, cancellationToken);
    var skills = await skillService.ListPublicAsync(locale, cancellationToken);
    var projects = await projectService.ListPublicAsync(locale, featuredOnly: true, cancellationToken);

    var model = BuildModel(locale, profile, privateContact, experiences, education, skills, projects);
    return new CvFile(CvFormat.FileName(profile.FullName, locale), CvDocument.Render(model));
  }

  /// <summary>
  /// Maps the public read models to the CV model, adding <paramref name="privateContact"/> for the
  /// <c>Full</c> variant; pure, so the mapping is tested without a database.
  /// </summary>
  internal static CvModel BuildModel(
    Locale locale,
    PublicProfile profile,
    CvPrivateContact? privateContact,
    IReadOnlyList<PublicExperience> experiences,
    IReadOnlyList<PublicEducation> education,
    IReadOnlyList<PublicSkillGroup> skills,
    IReadOnlyList<PublicProjectCard> projects)
  {
    var texts = CvTexts.For(locale);
    return new CvModel(
      locale,
      profile.FullName,
      profile.TitleLine,
      ContactLines(profile, privateContact),
      profile.Intro,
      experiences.Select(item => new CvEntry(
        CvFormat.Period(item.StartDate, item.EndDate, locale),
        item.RoleTitle,
        Join(item.Company, item.Location),
        item.Summary,
        item.Bullets,
        item.StackLine)).ToList(),
      education.Select(item => new CvEntry(
        CvFormat.Period(item.StartDate, item.EndDate, locale),
        item.Degree,
        Join(item.Institution, item.Location),
        item.Summary,
        [],
        item.Grade is null ? null : $"{texts.Grade} {item.Grade}")).ToList(),
      skills.Select(group => new CvSkillGroup(
        group.Name,
        group.Skills.Select(skill => CvFormat.Skill(skill.Name, skill.SinceYear, skill.Qualifier, locale)).ToList()))
        .ToList(),
      projects.Select(project => new CvProject(
        project.Title,
        project.Teaser,
        project.StackTags.Count == 0 ? null : string.Join(" · ", project.StackTags))).ToList());
  }

  private static List<string> ContactLines(PublicProfile profile, CvPrivateContact? privateContact)
  {
    var lines = new List<string> { profile.Email };
    if (privateContact?.Phone is { } phone)
    {
      lines.Add(phone);
    }

    // The street address names the city anyway, so it takes the location's place.
    var hasAddress = privateContact is { AddressLines.Count: > 0 };
    lines.Add(hasAddress ? string.Join(", ", privateContact!.AddressLines) : profile.Location);
    lines.AddRange(new[] { profile.GitHubUrl, profile.LinkedInUrl }.OfType<string>().Select(WithoutScheme));
    return lines;
  }

  private static string WithoutScheme(string url)
    => url.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
      .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
      .TrimEnd('/');

  private static string Join(string organisation, string? location)
    => string.IsNullOrWhiteSpace(location) ? organisation : $"{organisation}, {location}";
}
