using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The two startup variants besides the normal one: the OpenApi environment that generates the
/// client documents without a database, and production with an OTLP endpoint configured.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class HostingModesTests(PostgresFixture postgres)
{
  [Fact]
  public async Task The_OpenApi_environment_starts_without_a_database_and_reports_it_unreachable()
  {
    await using var factory = new ApiFactory(connectionString: null, environment: "OpenApi");
    using var client = factory.CreateClient();

    var health = await client.GetAsync("/api/health");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
  }

  [Fact]
  public void A_missing_connection_string_outside_OpenApi_fails_fast()
  {
    using var factory = new ApiFactory(connectionString: null);

    var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

    Assert.Contains("ConnectionStrings__Default", exception.ToString());
  }

  [PostgresFact]
  public async Task Telemetry_is_wired_only_when_an_OTLP_endpoint_is_configured()
  {
    await using var silent = new ApiFactory(await postgres.CreateDatabaseAsync());
    await using var exporting = new ApiFactory(await postgres.CreateDatabaseAsync(), otlpEndpoint: "http://127.0.0.1:4317");
    using var silentClient = silent.CreateClient();
    using var exportingClient = exporting.CreateClient();

    var silentHealth = await silentClient.GetAsync("/api/health");
    var exportingHealth = await exportingClient.GetAsync("/api/health");

    Assert.Equal(HttpStatusCode.OK, silentHealth.StatusCode);
    Assert.Equal(HttpStatusCode.OK, exportingHealth.StatusCode);
    Assert.Null(silent.Services.GetService<TracerProvider>());
    Assert.NotNull(exporting.Services.GetService<TracerProvider>());
  }
}
