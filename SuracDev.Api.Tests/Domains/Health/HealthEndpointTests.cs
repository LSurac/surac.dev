using System.Net;
using System.Net.Http.Json;
using SuracDev.Api.Domains.Health.Controllers;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Health;

[Collection(PostgresCollection.Name)]
public sealed class HealthEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task Reports_ok_when_the_database_is_reachable()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/health");
    var body = await response.Content.ReadFromJsonAsync<HealthController.HealthResponse>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("ok", body?.Status);
  }
}
