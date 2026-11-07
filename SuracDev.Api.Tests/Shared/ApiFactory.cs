using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SuracDev.Api.Domains.Identity.Contracts;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The real application bound to one test database. Startup runs the migrations (a no-op on a
/// template copy) and the seeder, so every test starts with the seeded admin without a password.
/// </summary>
public sealed class ApiFactory(
  string? connectionString,
  string environment = "Testing",
  string? otlpEndpoint = null) : WebApplicationFactory<Program>
{
  public const string InitialPassword = "correct horse battery";

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment(environment);
    builder.UseSetting("ConnectionStrings:Default", connectionString ?? string.Empty);
    builder.UseSetting("DataProtection:KeyPath", string.Empty);
    builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint ?? string.Empty);
  }

  /// <summary>A client whose cookie container holds a fresh admin session (first login sets the password).</summary>
  public async Task<HttpClient> CreateLoggedInClientAsync(string password = InitialPassword)
  {
    var client = CreateClient();
    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(password));
    response.EnsureSuccessStatusCode();
    return client;
  }
}
