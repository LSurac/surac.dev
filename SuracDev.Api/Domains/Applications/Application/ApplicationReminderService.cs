using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Domain;

namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>
/// Where the reminder digest goes: an ntfy server and topic, with an optional access token. Without
/// a server the reminders are off, so local runs, tests and production without ntfy stay silent.
/// </summary>
public sealed class ReminderOptions
{
  public const string Section = "Reminders";

  /// <summary>Base URL of the ntfy server, e.g. <c>https://ntfy.example</c>; empty switches reminders off.</summary>
  public string? NtfyServer { get; set; }

  /// <summary>The topic the digest is published to.</summary>
  public string? NtfyTopic { get; set; }

  /// <summary>Access token for a protected topic; a secret, set from the environment only.</summary>
  public string? NtfyToken { get; set; }

  /// <summary>True when server and topic are both set.</summary>
  public bool IsEnabled => !string.IsNullOrWhiteSpace(NtfyServer) && !string.IsNullOrWhiteSpace(NtfyTopic);
}

/// <summary>
/// Sends the weekly digest of applications without reply (<see cref="ApplicationReminder"/>) to ntfy.
/// </summary>
public sealed class ApplicationReminderService(
  SuracDevDbContext dbContext,
  IHttpClientFactory httpClientFactory,
  IOptions<ReminderOptions> options)
{
  /// <summary>Name of the HTTP client that talks to ntfy.</summary>
  public const string HttpClientName = "ntfy";

  /// <summary>
  /// Publishes the digest for <paramref name="today"/> when reminders are on and at least one
  /// application is due; returns whether a notification went out.
  /// </summary>
  public async Task<bool> SendDigestAsync(DateOnly today, CancellationToken cancellationToken)
  {
    var settings = options.Value;
    if (!settings.IsEnabled)
    {
      return false;
    }

    var candidates = await dbContext.Applications.AsNoTracking()
      .Where(application => application.Status == ApplicationStatus.Sent)
      .Select(application => new ReminderCandidate(
        application.Company,
        application.Position,
        application.Status,
        application.AppliedOn))
      .ToListAsync(cancellationToken);
    var due = ApplicationReminder.Due(candidates, today);
    if (due.Count == 0)
    {
      return false;
    }

    var message = ApplicationReminder.Digest(due);
    using var request = new HttpRequestMessage(HttpMethod.Post, settings.NtfyServer!.TrimEnd('/'))
    {
      Content = JsonContent.Create(new
      {
        topic = settings.NtfyTopic,
        title = message.Title,
        message = message.Body,
        tags = new[] { "briefcase" },
      }),
    };
    if (!string.IsNullOrWhiteSpace(settings.NtfyToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.NtfyToken);
    }

    using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
    response.EnsureSuccessStatusCode();
    return true;
  }
}
