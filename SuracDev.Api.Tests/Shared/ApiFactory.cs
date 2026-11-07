using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Domain;
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

  /// <summary>Runs <paramref name="work"/> against the test database, for arranging rows the API cannot create yet.</summary>
  public async Task<T> WithDbContextAsync<T>(Func<SuracDevDbContext, Task<T>> work)
  {
    await using var scope = Services.CreateAsyncScope();
    return await work(scope.ServiceProvider.GetRequiredService<SuracDevDbContext>());
  }

  /// <summary>Stores a document row (without a file) and returns its id.</summary>
  public Task<long> AddDocumentAsync(
    DocumentKind kind = DocumentKind.Photo,
    DocumentVisibility visibility = DocumentVisibility.Public,
    string contentType = DocumentContentTypes.Png)
    => WithDbContextAsync(async dbContext =>
    {
      var document = Document.Create(
        new DocumentUpload("datei.png", contentType, 10, $"test/{Guid.NewGuid():N}", new string('a', 64), kind, visibility, null, null),
        DateTimeOffset.UtcNow);
      dbContext.Documents.Add(document);
      await dbContext.SaveChangesAsync();
      return document.Id;
    });
}
