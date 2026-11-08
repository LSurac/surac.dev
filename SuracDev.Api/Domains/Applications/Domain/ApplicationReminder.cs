namespace SuracDev.Api.Domains.Applications.Domain;

/// <summary>An application as the reminder rule needs it.</summary>
public sealed record ReminderCandidate(string Company, string Position, ApplicationStatus Status, DateOnly? AppliedOn);

/// <summary>A sent application still waiting for an answer.</summary>
public sealed record DueApplication(string Company, string Position, int DaysWaiting);

/// <summary>The text of one push notification.</summary>
public sealed record ReminderMessage(string Title, string Body);

/// <summary>
/// The weekly reminder of applications without reply (docs/content-model.md, "Reminders"): every
/// application still <c>Sent</c> fourteen days or more after <c>AppliedOn</c>, sent as one digest
/// on Monday from 07:00 UTC. Stateless on purpose: the digest repeats each week until the status
/// changes, which is what a reminder is for, and needs no column to remember what was sent.
/// </summary>
public static class ApplicationReminder
{
  /// <summary>Days after sending from which an application without reply is due.</summary>
  public const int WaitingDays = 14;

  private static readonly TimeOnly DigestTime = new(7, 0);

  /// <summary>The due applications, longest waiting first.</summary>
  public static IReadOnlyList<DueApplication> Due(IEnumerable<ReminderCandidate> candidates, DateOnly today)
    => candidates
      .Where(candidate => candidate.Status == ApplicationStatus.Sent && candidate.AppliedOn is not null)
      .Select(candidate => new DueApplication(
        candidate.Company,
        candidate.Position,
        today.DayNumber - candidate.AppliedOn!.Value.DayNumber))
      .Where(entry => entry.DaysWaiting >= WaitingDays)
      .OrderByDescending(entry => entry.DaysWaiting)
      .ToList();

  /// <summary>One notification naming every due application and how long it has waited.</summary>
  public static ReminderMessage Digest(IReadOnlyList<DueApplication> due)
    => new(
      $"{due.Count} {(due.Count == 1 ? "Bewerbung" : "Bewerbungen")} "
        + $"ohne Antwort seit {WaitingDays} Tagen oder länger",
      string.Join('\n', due.Select(entry => $"{entry.Company} – {entry.Position} ({entry.DaysWaiting} Tage)")));

  /// <summary>Whether the weekly digest is due at <paramref name="now"/>, given the day it last went out.</summary>
  public static bool IsDigestDue(DateTimeOffset now, DateOnly? lastSent)
  {
    var utc = now.UtcDateTime;
    var today = DateOnly.FromDateTime(utc);
    return utc.DayOfWeek == DayOfWeek.Monday
      && TimeOnly.FromDateTime(utc) >= DigestTime
      && lastSent != today;
  }
}
