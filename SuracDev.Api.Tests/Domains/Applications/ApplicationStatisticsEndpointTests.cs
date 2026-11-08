using System.Net.Http.Json;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Tests.Shared;
using static SuracDev.Api.Tests.Domains.Applications.ApplicationEndpointTests;

namespace SuracDev.Api.Tests.Domains.Applications;

[Collection(PostgresCollection.Name)]
public sealed class ApplicationStatisticsEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task The_statistics_count_link_activity_per_application_and_the_response_rate()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var opened = await CreateApplicationAsync(admin);
    var quiet = await CreateApplicationAsync(admin);
    await admin.PutAsJsonAsync(
      $"/api/admin/applications/{quiet}",
      NewApplication with { Status = ApplicationStatus.Draft },
      ApiJson.Options);
    await admin.PutAsJsonAsync(
      $"/api/admin/applications/{opened}",
      NewApplication with { Status = ApplicationStatus.Interview },
      ApiJson.Options);
    var first = await CreateLinkAsync(admin, opened, NewLink);
    var second = await CreateLinkAsync(admin, opened, NewLink);

    using (var visitor = factory.CreateClient())
    {
      await visitor.GetAsync($"/api/links/{first.Token}");
      await visitor.GetAsync($"/api/links/{first.Token}?locale=en");
      await visitor.GetAsync($"/api/links/{second.Token}/cv.pdf");
    }

    var statistics = await admin.GetFromJsonAsync<ApplicationStatistics>(
      "/api/admin/application-statistics",
      ApiJson.Options);

    Assert.Equal(1, statistics!.SentCount);
    Assert.Equal(1, statistics.RespondedCount);
    Assert.Equal(1d, statistics.ResponseRate);
    Assert.Equal(1, statistics.Pipeline.Single(entry => entry.Status == ApplicationStatus.Draft).Count);
    Assert.Equal(1, statistics.Pipeline.Single(entry => entry.Status == ApplicationStatus.Interview).Count);
    Assert.Equal([quiet, opened], statistics.Applications.Select(activity => activity.ApplicationId));
    var active = statistics.Applications.Single(activity => activity.ApplicationId == opened);
    Assert.Equal(2, active.LinkCount);
    Assert.Equal(2, active.Opens);
    Assert.Equal(1, active.PdfDownloads);
    Assert.Equal(0, active.DocumentDownloads);
    Assert.NotNull(active.FirstOpenedAt);
    Assert.InRange(active.MinutesToFirstOpen!.Value, 0, 1);
    var silent = statistics.Applications.Single(activity => activity.ApplicationId == quiet);
    Assert.Equal(0, silent.Opens);
    Assert.Null(silent.FirstOpenedAt);
    Assert.Null(silent.MinutesToFirstOpen);
  }
}
