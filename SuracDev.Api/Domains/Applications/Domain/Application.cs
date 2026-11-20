using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Applications.Domain;

/// <summary>The pipeline of an application. Stored as int; <c>Unknown</c> is never written and values are
/// frozen.</summary>
public enum ApplicationStatus
{
  Unknown = 0,
  Draft = 1,
  Sent = 2,
  Replied = 3,
  Interview = 4,
  Offer = 5,
  Rejected = 6,
  Withdrawn = 7,
}

/// <summary>The data of an application as the admin enters it.</summary>
public sealed record ApplicationDetails(
  string? Company,
  string? Position,
  string? JobAdUrl,
  DateOnly? AppliedOn,
  ApplicationStatus Status,
  string? Notes);

/// <summary>
/// One application to one employer: the maintainer's mini ATS. Admin only; its links are
/// separate entities so that revoking or expiring one never touches the application.
/// </summary>
public sealed class Application
{
  private Application()
  {
  }

  public long Id { get; private set; }

  public string Company { get; private set; } = null!;

  public string Position { get; private set; } = null!;

  public string? JobAdUrl { get; private set; }

  public DateOnly? AppliedOn { get; private set; }

  public ApplicationStatus Status { get; private set; }

  /// <summary>Private Markdown notes; never leave the admin area.</summary>
  public string? Notes { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public DateTimeOffset UpdatedAt { get; private set; }

  /// <summary>Creates an application stamped with <paramref name="now"/>.</summary>
  public static Application Create(ApplicationDetails details, DateTimeOffset now)
  {
    var application = new Application { CreatedAt = now };
    application.Update(details, now);
    return application;
  }

  /// <summary>
  /// Replaces the data after validating it and moves the update stamp. Every status after
  /// <see cref="ApplicationStatus.Draft"/> needs <see cref="AppliedOn"/>, so a sent application is never
  /// left out of the reminder digest (<see cref="ApplicationReminder"/>).
  /// </summary>
  public void Update(ApplicationDetails details, DateTimeOffset now)
  {
    var company = Require.Text(details.Company, "company", TextLimits.Name);
    var position = Require.Text(details.Position, "position", TextLimits.Name);
    var jobAdUrl = Require.OptionalUrl(details.JobAdUrl, "jobAdUrl");
    var status = Require.Known(details.Status, "status");
    var notes = Require.OptionalText(details.Notes, "notes", TextLimits.Markdown);
    if (status != ApplicationStatus.Draft && details.AppliedOn is null)
    {
      throw new DomainValidationException("appliedOn", "Required once the application is no longer a draft.");
    }

    Company = company;
    Position = position;
    JobAdUrl = jobAdUrl;
    AppliedOn = details.AppliedOn;
    Status = status;
    Notes = notes;
    UpdatedAt = now;
  }
}
