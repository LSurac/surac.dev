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
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Pdf.Application;

/// <summary>A rendered CV with the file name it is downloaded under.</summary>
public sealed record CvFile(string FileName, byte[] Content);

/// <summary>
/// Renders the CV variants on demand. The <c>Public</c> variant reads exactly what the open site
/// shows, through the same public read services, so it can never contain phone, street address or
/// hidden entries (docs/content-model.md, "Visibility rules").
/// </summary>
public sealed class CvService(
  ProfileService profileService,
  ExperienceService experienceService,
  EducationService educationService,
  SkillService skillService,
  ProjectService projectService)
{
  /// <summary>The public CV in <paramref name="locale"/>, with German fallback like the site.</summary>
  public async Task<CvFile> RenderPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var profile = await profileService.GetPublicAsync(locale, cancellationToken);
    var experiences = await experienceService.ListPublicAsync(locale, cancellationToken);
    var education = await educationService.ListPublicAsync(locale, cancellationToken);
    var skills = await skillService.ListPublicAsync(locale, cancellationToken);
    var projects = await projectService.ListPublicAsync(locale, featuredOnly: true, cancellationToken);

    var model = BuildPublicModel(locale, profile, experiences, education, skills, projects);
    return new CvFile(CvFormat.FileName(profile.FullName, locale), CvDocument.Render(model));
  }

  /// <summary>Maps the public read models to the CV model; pure, so the mapping is tested without a database.</summary>
  internal static CvModel BuildPublicModel(
    Locale locale,
    PublicProfile profile,
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
      ContactLines(profile),
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

  private static List<string> ContactLines(PublicProfile profile)
  {
    var lines = new List<string> { profile.Email, profile.Location };
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
