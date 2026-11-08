using SuracDev.Api.Domains.Applications.Domain;

namespace SuracDev.Api.Tests.Domains.Applications;

public sealed class ApplicationPipelineTests
{
  [Fact]
  public void The_response_rate_counts_replies_interviews_offers_and_rejections_among_sent_applications()
  {
    var rate = ApplicationPipeline.ResponseRate(
    [
      ApplicationStatus.Draft,
      ApplicationStatus.Sent,
      ApplicationStatus.Sent,
      ApplicationStatus.Replied,
      ApplicationStatus.Interview,
      ApplicationStatus.Offer,
      ApplicationStatus.Rejected,
      ApplicationStatus.Withdrawn,
    ]);

    Assert.Equal(7, rate.Sent);
    Assert.Equal(4, rate.Responded);
    Assert.Equal(4d / 7, rate.Rate);
  }

  [Fact]
  public void Without_sent_applications_there_is_no_rate()
  {
    var rate = ApplicationPipeline.ResponseRate([ApplicationStatus.Draft]);

    Assert.Equal(0, rate.Sent);
    Assert.Null(rate.Rate);
  }

  [Fact]
  public void The_pipeline_lists_every_known_status_in_order_with_its_count()
  {
    var pipeline = ApplicationPipeline.Count([ApplicationStatus.Sent, ApplicationStatus.Sent, ApplicationStatus.Offer]);

    Assert.Equal(Enum.GetValues<ApplicationStatus>().Skip(1), pipeline.Select(entry => entry.Status));
    Assert.Equal(2, pipeline.Single(entry => entry.Status == ApplicationStatus.Sent).Count);
    Assert.Equal(0, pipeline.Single(entry => entry.Status == ApplicationStatus.Draft).Count);
  }

  [Fact]
  public void The_time_to_first_open_runs_from_the_first_link_to_the_first_opening_in_whole_minutes()
  {
    var created = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    Assert.Equal(90, ApplicationPipeline.MinutesToFirstOpen(created, created.AddMinutes(90).AddSeconds(59)));
    Assert.Null(ApplicationPipeline.MinutesToFirstOpen(created, null));
    Assert.Null(ApplicationPipeline.MinutesToFirstOpen(null, created));
  }
}
