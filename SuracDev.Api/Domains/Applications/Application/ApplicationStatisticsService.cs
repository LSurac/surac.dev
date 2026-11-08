using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;

namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>
/// Builds the statistics page from applications, links and link events. Counting and grouping run
/// in SQL; only the per-application rows are joined in memory.
/// </summary>
public sealed class ApplicationStatisticsService(SuracDevDbContext dbContext)
{
  /// <summary>The status pipeline, the response rate and the link activity per application.</summary>
  public async Task<ApplicationStatistics> GetAsync(CancellationToken cancellationToken)
  {
    var applications = await dbContext.Applications.AsNoTracking()
      .OrderByDescending(application => application.CreatedAt)
      .ThenByDescending(application => application.Id)
      .Select(application => new { application.Id, application.Company, application.Position, application.Status, application.AppliedOn })
      .ToListAsync(cancellationToken);

    var links = await dbContext.ApplicationLinks.AsNoTracking()
      .GroupBy(link => link.ApplicationId)
      .Select(group => new { ApplicationId = group.Key, Count = group.Count(), FirstCreatedAt = group.Min(link => link.CreatedAt) })
      .ToDictionaryAsync(entry => entry.ApplicationId, cancellationToken);

    var events = await (
        from linkEvent in dbContext.LinkEvents.AsNoTracking()
        join link in dbContext.ApplicationLinks on linkEvent.LinkId equals link.Id
        group linkEvent by link.ApplicationId into perApplication
        select new
        {
          ApplicationId = perApplication.Key,
          Opens = perApplication.Count(e => e.Kind == LinkEventKind.Opened),
          PdfDownloads = perApplication.Count(e => e.Kind == LinkEventKind.PdfDownloaded),
          DocumentDownloads = perApplication.Count(e => e.Kind == LinkEventKind.DocumentDownloaded),
          FirstOpenedAt = perApplication.Where(e => e.Kind == LinkEventKind.Opened).Min(e => (DateTimeOffset?)e.OccurredAt),
        })
      .ToDictionaryAsync(entry => entry.ApplicationId, cancellationToken);

    var statuses = applications.Select(application => application.Status).ToList();
    var rate = ApplicationPipeline.ResponseRate(statuses);
    var activity = applications.Select(application =>
    {
      var linkStats = links.GetValueOrDefault(application.Id);
      var eventStats = events.GetValueOrDefault(application.Id);
      return new ApplicationActivity(
        application.Id,
        application.Company,
        application.Position,
        application.Status,
        application.AppliedOn,
        linkStats?.Count ?? 0,
        eventStats?.Opens ?? 0,
        eventStats?.PdfDownloads ?? 0,
        eventStats?.DocumentDownloads ?? 0,
        eventStats?.FirstOpenedAt,
        ApplicationPipeline.MinutesToFirstOpen(linkStats?.FirstCreatedAt, eventStats?.FirstOpenedAt));
    }).ToList();

    return new ApplicationStatistics(rate.Sent, rate.Responded, rate.Rate, ApplicationPipeline.Count(statuses), activity);
  }
}
