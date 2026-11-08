using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Identity.Contracts;
using SuracDev.Api.Tests.Shared;
using static SuracDev.Api.Tests.Domains.Applications.ApplicationEndpointTests;

namespace SuracDev.Api.Tests.Domains.Applications;

[Collection(PostgresCollection.Name)]
public sealed class ApplicationReminderEndpointTests(PostgresFixture postgres)
{
  private static readonly DateOnly Today = new(2026, 11, 16);

  [PostgresFact]
  public async Task The_digest_publishes_the_waiting_applications_to_the_configured_topic()
  {
    var ntfy = new RecordingHandler();
    await using var api = new ApiFactory(await postgres.CreateDatabaseAsync());
    await using var factory = WithReminders(api, ntfy);
    using (var admin = await LogInAsync(factory))
    {
      await admin.PostAsJsonAsync(
        "/api/admin/applications",
        NewApplication with { Company = "Wartet AG", AppliedOn = Today.AddDays(-20) },
        ApiJson.Options);
      await admin.PostAsJsonAsync(
        "/api/admin/applications",
        NewApplication with { Company = "Frisch AG", AppliedOn = Today.AddDays(-3) },
        ApiJson.Options);
    }

    var sent = await SendAsync(factory);

    Assert.True(sent);
    var request = Assert.Single(ntfy.Requests);
    Assert.Equal("http://ntfy.test/", request.Uri);
    Assert.Equal("Bearer test-token", request.Authorization);
    using var json = JsonDocument.Parse(request.Body);
    Assert.Equal("bewerbungen", json.RootElement.GetProperty("topic").GetString());
    Assert.Equal("Wartet AG – Fullstack-Entwickler (20 Tage)", json.RootElement.GetProperty("message").GetString());
  }

  [PostgresFact]
  public async Task Nothing_is_published_while_no_application_is_due()
  {
    var ntfy = new RecordingHandler();
    await using var api = new ApiFactory(await postgres.CreateDatabaseAsync());
    await using var factory = WithReminders(api, ntfy);

    var sent = await SendAsync(factory);

    Assert.False(sent);
    Assert.Empty(ntfy.Requests);
  }

  [PostgresFact]
  public async Task Without_an_ntfy_server_reminders_are_off()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync(
        "/api/admin/applications",
        NewApplication with { AppliedOn = Today.AddDays(-30) },
        ApiJson.Options);
    }

    var sent = await SendAsync(factory);

    Assert.False(sent);
    Assert.DoesNotContain(
      factory.Services.GetServices<IHostedService>(),
      service => service is ApplicationReminderWorker);
  }

  private static async Task<HttpClient> LogInAsync(WebApplicationFactory<Program> factory)
  {
    var client = factory.CreateClient();
    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.InitialPassword));
    response.EnsureSuccessStatusCode();
    return client;
  }

  private static async Task<bool> SendAsync(WebApplicationFactory<Program> factory)
  {
    await using var scope = factory.Services.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<ApplicationReminderService>()
      .SendDigestAsync(Today, CancellationToken.None);
  }

  private static WebApplicationFactory<Program> WithReminders(
    ApiFactory factory,
    RecordingHandler ntfy)
    => factory.WithWebHostBuilder(builder =>
    {
      builder.UseSetting("Reminders:NtfyServer", "http://ntfy.test");
      builder.UseSetting("Reminders:NtfyTopic", "bewerbungen");
      builder.UseSetting("Reminders:NtfyToken", "test-token");
      builder.ConfigureTestServices(services =>
      {
        // The schedule is tested as a pure rule; here only the sending is exercised.
        services.RemoveWhere<IHostedService>(
          descriptor => descriptor.ImplementationType == typeof(ApplicationReminderWorker));
        services.AddHttpClient(ApplicationReminderService.HttpClientName)
          .ConfigurePrimaryHttpMessageHandler(() => ntfy);
      });
    });

  private sealed record RecordedRequest(string Uri, string? Authorization, string Body);

  private sealed class RecordingHandler : HttpMessageHandler
  {
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      Requests.Add(new RecordedRequest(
        request.RequestUri!.ToString(),
        request.Headers.Authorization?.ToString(),
        await request.Content!.ReadAsStringAsync(cancellationToken)));
      return new HttpResponseMessage(HttpStatusCode.OK);
    }
  }
}

internal static class ServiceCollectionTestExtensions
{
  public static void RemoveWhere<TService>(this IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
  {
    foreach (var descriptor in services.Where(d => d.ServiceType == typeof(TService) && predicate(d)).ToList())
    {
      services.Remove(descriptor);
    }
  }
}
