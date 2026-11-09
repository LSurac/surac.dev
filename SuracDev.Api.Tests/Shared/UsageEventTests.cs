using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Identity.Contracts;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The usage events the maintainer's Seq dashboards are built on (the same shape as sk-sulzfeld and
/// Sanctuary of Sound): one line per proxied API request, one per page view, one per login attempt.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UsageEventTests(PostgresFixture postgres)
{
  private const string NginxContainer = "172.18.0.5";
  private const string Visitor = "203.0.113.57";

  [PostgresFact]
  public async Task A_page_view_becomes_one_line_with_area_referrer_and_the_shortened_address()
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();

    var response = await client.SendAsync(Proxied(HttpMethod.Post, "/api/public/page-views",
      new { path = "/en/projects/kassensturz", referrerHost = "www.google.com" }));

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var line = Assert.Single(log.Lines, l => l.Template.StartsWith("Page view", StringComparison.Ordinal));
    Assert.Equal("/en/projects/kassensturz", line["PagePath"]);
    Assert.Equal("public", line["PageArea"]);
    Assert.Equal("anonymous", line["UserName"]);
    Assert.Equal("203.0.113.0", line["ClientIp"]);
    Assert.Equal("www.google.com", line["ReferrerHost"]);
  }

  [PostgresFact]
  public async Task A_page_view_of_an_application_link_never_logs_its_token()
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();
    var token = ApplicationLinkToken.Generate().Token;

    await client.SendAsync(Proxied(HttpMethod.Post, "/api/public/page-views", new { path = $"/b/{token}" }));

    var line = Assert.Single(log.Lines, l => l.Template.StartsWith("Page view", StringComparison.Ordinal));
    Assert.Equal("/b/{token}", line["PagePath"]);
    Assert.Equal("link", line["PageArea"]);
    Assert.DoesNotContain(log.Lines, l => l.Text.Contains(token, StringComparison.Ordinal));
  }

  [PostgresTheory]
  [InlineData("/projekte?token=x")]
  [InlineData("/projekte#x")]
  [InlineData("projekte")]
  [InlineData("/zeile\nForged line")]
  public async Task A_page_view_with_anything_but_a_plain_site_path_is_refused(string path)
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();

    var response = await client.SendAsync(Proxied(HttpMethod.Post, "/api/public/page-views", new { path }));

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.DoesNotContain(log.Lines, l => l.Template.StartsWith("Page view", StringComparison.Ordinal));
  }

  [PostgresFact]
  public async Task A_proxied_request_is_logged_with_path_status_and_visitor()
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();
    var token = ApplicationLinkToken.Generate().Token;

    await client.SendAsync(Proxied(HttpMethod.Get, "/api/public/profile?locale=en"));
    await client.SendAsync(Proxied(HttpMethod.Get, $"/api/links/{token}"));
    await client.SendAsync(Proxied(HttpMethod.Get, "/api/health"));

    var requests = log.Lines.Where(l => l.Template.Contains("answered {StatusCode}", StringComparison.Ordinal)).ToList();
    Assert.Equal(["/api/public/profile", "/api/links/{token}"], requests.Select(l => l["RequestPath"]));
    Assert.All(requests, l => Assert.Equal(LogLevel.Information, l.Level));
    Assert.Equal("GET", requests[0]["RequestMethod"]);
    Assert.Equal(200, requests[0]["StatusCode"]);
    Assert.Equal("anonymous", requests[0]["UserName"]);
    Assert.Equal("203.0.113.0", requests[0]["ClientIp"]);
    Assert.DoesNotContain(log.Lines, l => l.Text.Contains(token, StringComparison.Ordinal));
  }

  [PostgresFact]
  public async Task A_request_from_inside_the_stack_stays_below_information()
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();

    // The SSR process calls the API directly, without X-Forwarded-For; its page view covers it.
    await client.GetAsync("/api/public/profile?locale=de");

    var line = Assert.Single(log.Lines, l => l.Template.Contains("answered {StatusCode}", StringComparison.Ordinal));
    Assert.Equal(LogLevel.Debug, line.Level);
  }

  [PostgresFact]
  public async Task Logins_are_logged_with_outcome_and_the_full_address()
  {
    var (factory, log) = await CreateAsync();
    await using var _ = factory;
    using var client = factory.CreateClient();

    await client.SendAsync(Proxied(HttpMethod.Post, "/api/auth/login", new LoginRequest(ApiFactory.InitialPassword)));
    await client.SendAsync(Proxied(HttpMethod.Post, "/api/auth/login", new LoginRequest("wrong horse battery")));

    var succeeded = Assert.Single(log.Lines, l => l.Text.StartsWith("Login succeeded", StringComparison.Ordinal));
    var failed = Assert.Single(log.Lines, l => l.Text.StartsWith("Failed login", StringComparison.Ordinal));
    Assert.Equal(Visitor, succeeded["ClientIp"]);
    Assert.Equal("admin", succeeded["LoginArea"]);
    Assert.Equal("password", succeeded["IdentifierKind"]);
    Assert.Equal(Visitor, failed["ClientIp"]);
    Assert.Equal("wrong_password", failed["FailureReason"]);
    Assert.Equal(LogLevel.Warning, failed.Level);
  }

  private async Task<(WebApplicationFactory<Program> Factory, LogSink Log)> CreateAsync()
  {
    var log = new LogSink();
    var factory = new ApiFactory(await postgres.CreateDatabaseAsync())
      .WithWebHostBuilder(builder => builder.ConfigureLogging(logging =>
      {
        // The application's own loggers down to Debug (to see the SSR requests); every other category
        // keeps the configured levels, so the sink sees what production exports, plus that.
        logging.AddFilter<LogSink>("SuracDev", LogLevel.Debug);
        logging.AddProvider(log);
      }));
    return (factory, log);
  }

  /// <summary>A request as nginx forwards it: from the nginx container, with the visitor in X-Forwarded-For.</summary>
  private static HttpRequestMessage Proxied(HttpMethod method, string url, object? body = null)
  {
    var request = new HttpRequestMessage(method, url);
    request.Headers.Add(ApiFactory.PeerAddressHeader, NginxContainer);
    request.Headers.Add("X-Forwarded-For", Visitor);
    if (body is not null)
    {
      request.Content = JsonContent.Create(body);
    }

    return request;
  }

  private sealed record LogLine(LogLevel Level, string Template, string Text, IReadOnlyDictionary<string, object?> Values)
  {
    public object? this[string key] => Values[key];
  }

  /// <summary>Keeps the structured lines of the usage loggers, the way an OTLP exporter sees them.</summary>
  private sealed class LogSink : ILoggerProvider
  {
    private readonly ConcurrentQueue<LogLine> lines = new();

    public IReadOnlyList<LogLine> Lines => [.. lines];

    public ILogger CreateLogger(string categoryName) => new Logger(lines);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<LogLine> lines) : ILogger
    {
      public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

      public bool IsEnabled(LogLevel logLevel) => true;

      public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
      {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
        {
          return;
        }

        var map = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        var template = map.TryGetValue("{OriginalFormat}", out var format) ? format as string ?? string.Empty : string.Empty;
        lines.Enqueue(new LogLine(logLevel, template, formatter(state, exception), map));
      }
    }
  }
}
