using SuracDev.Api.Shared.Application.Audience;

namespace SuracDev.Api.Tests.Shared;

public sealed class SectionOrderTests
{
  public static TheoryData<AudienceProfile> KnownProfiles =>
    new(Enum.GetValues<AudienceProfile>().Where(profile => profile != AudienceProfile.Unknown));

  [Theory]
  [MemberData(nameof(KnownProfiles))]
  public void Every_profile_shows_every_section_exactly_once(AudienceProfile profile)
  {
    var order = SectionOrder.For(profile);
    var allSections = Enum.GetValues<SiteSection>().Where(section => section != SiteSection.Unknown);

    Assert.Equal(allSections.OrderBy(section => section), order.OrderBy(section => section));
    Assert.Equal(order.Count, order.Distinct().Count());
  }

  [Theory]
  [MemberData(nameof(KnownProfiles))]
  public void Hero_opens_and_contact_closes_every_profile(AudienceProfile profile)
  {
    var order = SectionOrder.For(profile);

    Assert.Equal(SiteSection.Hero, order[0]);
    Assert.Equal(SiteSection.Contact, order[^1]);
  }

  [Fact]
  public void Recruiters_see_the_career_before_the_projects()
  {
    var order = SectionOrder.For(AudienceProfile.Recruiter);

    Assert.True(order.IndexOf(SiteSection.Career) < order.IndexOf(SiteSection.Projects));
  }

  [Fact]
  public void Tech_leads_see_the_projects_before_the_career()
  {
    var order = SectionOrder.For(AudienceProfile.TechLead);

    Assert.True(order.IndexOf(SiteSection.Projects) < order.IndexOf(SiteSection.Career));
  }

  [Fact]
  public void Unknown_profile_is_a_programming_error()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => SectionOrder.For(AudienceProfile.Unknown));
  }
}

file static class ReadOnlyListExtensions
{
  public static int IndexOf(this IReadOnlyList<SiteSection> list, SiteSection section)
  {
    for (var index = 0; index < list.Count; index++)
    {
      if (list[index] == section)
      {
        return index;
      }
    }

    return -1;
  }
}
