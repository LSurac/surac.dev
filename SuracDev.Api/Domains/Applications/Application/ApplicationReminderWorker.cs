namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>
/// Checks every quarter of an hour whether the weekly digest is due and sends it. Registered only
/// when reminders are configured. A failed send is logged and retried at the next check; a restart
/// on Monday after the digest went out sends it once more, which is accepted for a reminder.
/// </summary>
public sealed class ApplicationReminderWorker(
  IServiceScopeFactory scopeFactory,
  TimeProvider timeProvider,
  ILogger<ApplicationReminderWorker> logger) : BackgroundService
{
  private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

  private DateOnly? lastSent;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(Interval, timeProvider);
    do
    {
      await CheckAsync(stoppingToken);
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
  }

  private async Task CheckAsync(CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    if (!Domain.ApplicationReminder.IsDigestDue(now, lastSent))
    {
      return;
    }

    try
    {
      await using var scope = scopeFactory.CreateAsyncScope();
      var reminders = scope.ServiceProvider.GetRequiredService<ApplicationReminderService>();
      var today = DateOnly.FromDateTime(now.UtcDateTime);
      var sent = await reminders.SendDigestAsync(today, cancellationToken);
      lastSent = today;
      logger.LogInformation("Application reminder digest checked; notification sent: {Sent}", sent);
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
      && !cancellationToken.IsCancellationRequested)
    {
      logger.LogWarning(exception, "Application reminder digest could not be sent; retrying at the next check");
    }
  }
}
