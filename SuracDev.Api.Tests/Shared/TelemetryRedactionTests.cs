using System.Collections.Concurrent;
using System.Diagnostics;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Shared.Infrastructure.Telemetry;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// Application-link tokens are capabilities: whoever reads one can open the link. They must never
/// reach the telemetry store (docs/security-and-privacy.md, "Personal data inventory").
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TelemetryRedactionTests(PostgresFixture postgres)
{
  [Theory]
  [InlineData("/api/links/abc_DEF-123", "/api/links/{token}")]
  [InlineData("/api/links/abc_DEF-123/cv.pdf", "/api/links/{token}/cv.pdf")]
  [InlineData("/api/links/abc_DEF-123/documents/7", "/api/links/{token}/documents/7")]
  [InlineData("/api/public/profile", "/api/public/profile")]
  public void A_link_token_in_a_path_is_replaced_by_a_placeholder(string path, string expected)
  {
    Assert.Equal(expected, TelemetryRedaction.Path(path));
  }

  [PostgresFact]
  public async Task Request_spans_never_carry_the_token_of_an_application_link()
  {
    var stopped = new ConcurrentBag<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStopped = stopped.Add,
    };
    ActivitySource.AddActivityListener(listener);
    await using var factory = new ApiFactory(
      await postgres.CreateDatabaseAsync(),
      otlpEndpoint: "http://127.0.0.1:4317");
    using var client = factory.CreateClient();
    var token = ApplicationLinkToken.Generate().Token;

    await client.GetAsync($"/api/links/{token}/cv.pdf?locale=de");

    var span = Assert.Single(stopped, activity => Describe(activity).Contains("/api/links/", StringComparison.Ordinal));
    Assert.Equal("/api/links/{token}/cv.pdf", span.GetTagItem("url.path"));
    Assert.DoesNotContain(token, Describe(span), StringComparison.Ordinal);
  }

  private static string Describe(Activity activity)
    => string.Join(' ', activity.TagObjects.Select(tag => $"{tag.Key}={tag.Value}").Append(activity.DisplayName));
}
