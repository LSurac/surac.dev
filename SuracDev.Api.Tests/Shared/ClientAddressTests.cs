using System.Net;
using SuracDev.Api.Domains.Applications.Domain;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The client address the rate limits partition by, for the real proxy chains: production
/// (visitor → Caddy → nginx → API, nginx appending the Docker gateway), staging (visitor → nginx →
/// API) and server-side renders (the SSR process passes nginx's header on).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ClientAddressTests(PostgresFixture postgres)
{
  private const string NginxContainer = "172.18.0.5";
  private const string DockerGateway = "172.18.0.1";
  private const int LinkLimit = 30;

  [PostgresFact]
  public async Task Visitors_behind_caddy_and_nginx_are_limited_one_by_one()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var first = await ResolveAsync(client, LinkLimit + 1, NginxContainer, _ => $"203.0.113.1, {DockerGateway}");
    var second = await ResolveAsync(client, 1, NginxContainer, _ => $"203.0.113.2, {DockerGateway}");

    Assert.Equal(HttpStatusCode.TooManyRequests, first[^1]);
    Assert.Equal(HttpStatusCode.NotFound, second[0]);
  }

  [PostgresFact]
  public async Task Visitors_on_staging_are_limited_one_by_one()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var first = await ResolveAsync(client, LinkLimit + 1, NginxContainer, _ => "192.168.178.20");
    var second = await ResolveAsync(client, 1, NginxContainer, _ => "192.168.178.21");

    Assert.Equal(HttpStatusCode.TooManyRequests, first[^1]);
    Assert.Equal(HttpStatusCode.NotFound, second[0]);
  }

  [PostgresFact]
  public async Task A_forged_forwarded_address_does_not_escape_the_limit()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var statuses = await ResolveAsync(
      client, LinkLimit + 1, NginxContainer, attempt => $"198.51.100.{attempt}, 192.168.178.20");

    Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
  }

  private static async Task<List<HttpStatusCode>> ResolveAsync(
    HttpClient client,
    int attempts,
    string peer,
    Func<int, string> forwardedFor)
  {
    var statuses = new List<HttpStatusCode>();
    for (var attempt = 0; attempt < attempts; attempt++)
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{ApplicationLinkToken.Generate().Token}");
      request.Headers.Add(ApiFactory.PeerAddressHeader, peer);
      request.Headers.Add("X-Forwarded-For", forwardedFor(attempt));
      using var response = await client.SendAsync(request);
      statuses.Add(response.StatusCode);
    }

    return statuses;
  }
}
