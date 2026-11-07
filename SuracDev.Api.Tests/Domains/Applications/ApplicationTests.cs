using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Tests.Domains.Applications;

public sealed class ApplicationTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  private static readonly ApplicationDetails Details = new(
    Company: " Beispiel AG ",
    Position: "Fullstack-Entwickler",
    JobAdUrl: "https://example.org/job",
    AppliedOn: null,
    Status: ApplicationStatus.Draft,
    Notes: null);

  [Fact]
  public void A_new_application_is_stamped_on_creation()
  {
    var application = Application.Create(Details, Now);

    Assert.Equal("Beispiel AG", application.Company);
    Assert.Equal(Now, application.CreatedAt);
    Assert.Equal(Now, application.UpdatedAt);
  }

  [Fact]
  public void Company_position_and_a_known_status_are_required()
  {
    Assert.Throws<DomainValidationException>(() => Application.Create(Details with { Company = " " }, Now));
    Assert.Throws<DomainValidationException>(() => Application.Create(Details with { Position = null }, Now));
    Assert.Throws<DomainValidationException>(
      () => Application.Create(Details with { Status = ApplicationStatus.Unknown }, Now));
  }

  [Fact]
  public void Updating_moves_the_update_stamp_only()
  {
    var application = Application.Create(Details, Now);
    var later = Now.AddDays(2);

    application.Update(Details with { Status = ApplicationStatus.Sent, AppliedOn = new DateOnly(2026, 11, 9) }, later);

    Assert.Equal(ApplicationStatus.Sent, application.Status);
    Assert.Equal(Now, application.CreatedAt);
    Assert.Equal(later, application.UpdatedAt);
  }
}
