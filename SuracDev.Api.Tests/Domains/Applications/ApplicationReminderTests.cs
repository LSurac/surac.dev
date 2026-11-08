using SuracDev.Api.Domains.Applications.Domain;

namespace SuracDev.Api.Tests.Domains.Applications;

public sealed class ApplicationReminderTests
{
  private static readonly DateOnly Today = new(2026, 11, 16);

  [Fact]
  public void Sent_applications_without_reply_for_fourteen_days_are_due_oldest_first()
  {
    var due = ApplicationReminder.Due(
      [
        new ReminderCandidate("Fresh AG", "Entwickler", ApplicationStatus.Sent, Today.AddDays(-13)),
        new ReminderCandidate("Old GmbH", "Entwickler", ApplicationStatus.Sent, Today.AddDays(-30)),
        new ReminderCandidate("Due AG", "Backend", ApplicationStatus.Sent, Today.AddDays(-14)),
        new ReminderCandidate("Replied AG", "Entwickler", ApplicationStatus.Replied, Today.AddDays(-40)),
        new ReminderCandidate("Draft AG", "Entwickler", ApplicationStatus.Draft, null),
      ],
      Today);

    Assert.Equal(["Old GmbH", "Due AG"], due.Select(entry => entry.Company));
    Assert.Equal([30, 14], due.Select(entry => entry.DaysWaiting));
  }

  [Fact]
  public void The_digest_lists_each_application_with_its_waiting_time()
  {
    var message = ApplicationReminder.Digest(
      [new DueApplication("Old GmbH", "Entwickler", 30), new DueApplication("Due AG", "Backend", 14)]);

    Assert.Equal("2 Bewerbungen ohne Antwort seit 14 Tagen oder länger", message.Title);
    Assert.Equal("Old GmbH – Entwickler (30 Tage)\nDue AG – Backend (14 Tage)", message.Body);
  }

  [Theory]
  [InlineData("2026-11-16T07:00:00Z", null, true)]
  [InlineData("2026-11-16T06:59:00Z", null, false)]
  [InlineData("2026-11-17T09:00:00Z", null, false)]
  [InlineData("2026-11-16T09:00:00Z", "2026-11-16", false)]
  [InlineData("2026-11-23T07:30:00Z", "2026-11-16", true)]
  public void The_digest_goes_out_once_on_monday_from_seven_utc(string now, string? lastSent, bool expected)
  {
    var last = lastSent is null ? (DateOnly?)null : DateOnly.Parse(lastSent);

    Assert.Equal(expected, ApplicationReminder.IsDigestDue(DateTimeOffset.Parse(now), last));
  }
}
