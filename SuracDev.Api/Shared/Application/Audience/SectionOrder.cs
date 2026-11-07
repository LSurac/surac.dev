namespace SuracDev.Api.Shared.Application.Audience;

/// <summary>Who an application link is made for. Changes order and emphasis, never content.</summary>
public enum AudienceProfile
{
  Unknown = 0,
  Public = 1,
  Recruiter = 2,
  TechLead = 3,
}

/// <summary>The sections of the start page. Every profile shows all of them, in its own order.</summary>
public enum SiteSection
{
  Unknown = 0,
  Hero = 1,
  Facts = 2,
  Intro = 3,
  Projects = 4,
  Career = 5,
  Skills = 6,
  WorkingMode = 7,
  Contact = 8,
}

/// <summary>
/// The section order per audience profile. Configuration, not code paths: a profile is a list, and
/// the tests prove that every profile shows every section exactly once, hero first, contact last.
/// </summary>
public static class SectionOrder
{
  private static readonly IReadOnlyDictionary<AudienceProfile, IReadOnlyList<SiteSection>> Orders =
    new Dictionary<AudienceProfile, IReadOnlyList<SiteSection>>
    {
      [AudienceProfile.Public] =
      [
        SiteSection.Hero, SiteSection.Facts, SiteSection.Intro, SiteSection.Career,
        SiteSection.Projects, SiteSection.Skills, SiteSection.WorkingMode, SiteSection.Contact,
      ],
      [AudienceProfile.Recruiter] =
      [
        SiteSection.Hero, SiteSection.Facts, SiteSection.Intro, SiteSection.Career,
        SiteSection.Skills, SiteSection.Projects, SiteSection.WorkingMode, SiteSection.Contact,
      ],
      [AudienceProfile.TechLead] =
      [
        SiteSection.Hero, SiteSection.Facts, SiteSection.Projects, SiteSection.WorkingMode,
        SiteSection.Skills, SiteSection.Career, SiteSection.Intro, SiteSection.Contact,
      ],
    };

  public static IReadOnlyList<SiteSection> For(AudienceProfile profile)
    => Orders.TryGetValue(profile, out var order)
      ? order
      : throw new ArgumentOutOfRangeException(nameof(profile), profile, "No section order for this profile.");
}
