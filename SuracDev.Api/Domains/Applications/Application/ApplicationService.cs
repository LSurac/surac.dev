using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Applications.Application;

/// <summary>The use cases of the mini ATS: list, read, create, replace and delete applications.</summary>
public sealed class ApplicationService(SuracDevDbContext dbContext, TimeProvider timeProvider)
{
  /// <summary>Every application, newest first.</summary>
  public async Task<IReadOnlyList<AdminApplication>> ListAsync(CancellationToken cancellationToken)
    => await dbContext.Applications.AsNoTracking()
      .OrderByDescending(application => application.CreatedAt)
      .ThenByDescending(application => application.Id)
      .Select(application => new AdminApplication(
        application.Id,
        application.Company,
        application.Position,
        application.JobAdUrl,
        application.AppliedOn,
        application.Status,
        application.Notes,
        application.CreatedAt,
        application.UpdatedAt))
      .ToListAsync(cancellationToken);

  /// <summary>One application.</summary>
  /// <exception cref="DomainNotFoundException">No application has this id.</exception>
  public async Task<AdminApplication> GetAsync(long id, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates an application.</summary>
  /// <exception cref="DomainValidationException">A field breaks an application rule.</exception>
  public async Task<AdminApplication> CreateAsync(ApplicationForm form, CancellationToken cancellationToken)
  {
    var application = Domain.Application.Create(ToDetails(form), timeProvider.GetUtcNow());
    dbContext.Applications.Add(application);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ToAdmin(application);
  }

  /// <summary>Replaces an application's data.</summary>
  /// <exception cref="DomainNotFoundException">No application has this id.</exception>
  /// <exception cref="DomainValidationException">A field breaks an application rule.</exception>
  public async Task UpdateAsync(long id, ApplicationForm form, CancellationToken cancellationToken)
  {
    var application = await FindAsync(id, tracked: true, cancellationToken);
    application.Update(ToDetails(form), timeProvider.GetUtcNow());
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes an application with its links and their events; the documents stay.</summary>
  /// <exception cref="DomainNotFoundException">No application has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var application = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.Applications.Remove(application);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<Domain.Application> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = tracked ? dbContext.Applications : dbContext.Applications.AsNoTracking();
    return await query.SingleOrDefaultAsync(application => application.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Application {id} does not exist.");
  }

  private static ApplicationDetails ToDetails(ApplicationForm form)
    => new(form.Company, form.Position, form.JobAdUrl, form.AppliedOn, form.Status, form.Notes);

  private static AdminApplication ToAdmin(Domain.Application application)
    => new(
      application.Id,
      application.Company,
      application.Position,
      application.JobAdUrl,
      application.AppliedOn,
      application.Status,
      application.Notes,
      application.CreatedAt,
      application.UpdatedAt);
}
