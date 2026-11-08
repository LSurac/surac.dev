namespace SuracDev.Api.Domains.Applications.Domain;

/// <summary>How many sent applications got a response, and the rate; <c>Rate</c> is null when nothing was
/// sent.</summary>
public sealed record ResponseRate(int Sent, int Responded, double? Rate);

/// <summary>The number of applications in one status.</summary>
public sealed record StatusCount(ApplicationStatus Status, int Count);

/// <summary>
/// The rules behind the statistics page. Every status except <c>Draft</c> counts as sent; a reply,
/// an interview, an offer and a rejection count as a response. <c>Withdrawn</c> counts as sent
/// without response, because the status no longer says whether a reply came first.
/// </summary>
public static class ApplicationPipeline
{
  /// <summary>Whether an application in <paramref name="status"/> has left the maintainer's desk.</summary>
  public static bool IsSent(ApplicationStatus status)
    => status is not (ApplicationStatus.Unknown or ApplicationStatus.Draft);

  /// <summary>Whether the employer has answered an application in <paramref name="status"/>.</summary>
  public static bool HasResponse(ApplicationStatus status)
    => status is ApplicationStatus.Replied
      or ApplicationStatus.Interview
      or ApplicationStatus.Offer
      or ApplicationStatus.Rejected;

  /// <summary>The response rate over the given statuses.</summary>
  public static ResponseRate ResponseRate(IReadOnlyCollection<ApplicationStatus> statuses)
  {
    var sent = statuses.Count(IsSent);
    var responded = statuses.Count(HasResponse);
    return new ResponseRate(sent, responded, sent == 0 ? null : (double)responded / sent);
  }

  /// <summary>Every known status in pipeline order with the number of applications in it, zeros included.</summary>
  public static IReadOnlyList<StatusCount> Count(IReadOnlyCollection<ApplicationStatus> statuses)
    => Enum.GetValues<ApplicationStatus>()
      .Where(status => status != ApplicationStatus.Unknown)
      .Select(status => new StatusCount(status, statuses.Count(candidate => candidate == status)))
      .ToList();

  /// <summary>Whole minutes from the first link's creation to the first opening; null until both exist.</summary>
  public static int? MinutesToFirstOpen(DateTimeOffset? firstLinkCreatedAt, DateTimeOffset? firstOpenedAt)
    => firstLinkCreatedAt is { } created && firstOpenedAt is { } opened
      ? (int)Math.Floor((opened - created).TotalMinutes)
      : null;
}
