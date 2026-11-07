using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Career.Application;

/// <summary>The use cases of career stations: the public timeline and the admin's CRUD.</summary>
public sealed class ExperienceService(SuracDevDbContext dbContext)
{
  /// <summary>Visible stations, newest first, in <paramref name="locale"/> with German fallback.</summary>
  public async Task<IReadOnlyList<PublicExperience>> ListPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var stations = await Ordered(dbContext.Experiences.AsNoTracking().Include(e => e.Translations))
      .Where(experience => experience.IsVisible)
      .ToListAsync(cancellationToken);

    return stations.Select(experience => ToPublic(experience, locale)).ToList();
  }

  /// <summary>Every station including hidden ones, newest first.</summary>
  public async Task<IReadOnlyList<AdminExperience>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var stations = await Ordered(dbContext.Experiences.AsNoTracking().Include(e => e.Translations))
      .ToListAsync(cancellationToken);
    return stations.Select(ToAdmin).ToList();
  }

  /// <summary>One station for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No station has this id.</exception>
  public async Task<AdminExperience> GetAdminAsync(long id, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates a station and returns it as stored.</summary>
  /// <exception cref="DomainValidationException">A field breaks a station rule.</exception>
  public async Task<AdminExperience> CreateAsync(ExperienceForm form, CancellationToken cancellationToken)
  {
    var experience = Experience.Create(ToDetails(form), ToTexts(form.De));
    experience.SetTexts(Locale.English, ToTexts(form.En));
    dbContext.Experiences.Add(experience);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ToAdmin(experience);
  }

  /// <summary>Replaces a station with the given fields.</summary>
  /// <exception cref="DomainNotFoundException">No station has this id.</exception>
  /// <exception cref="DomainValidationException">A field breaks a station rule.</exception>
  public async Task UpdateAsync(long id, ExperienceForm form, CancellationToken cancellationToken)
  {
    var experience = await FindAsync(id, tracked: true, cancellationToken);
    experience.UpdateDetails(ToDetails(form));
    experience.SetTexts(Locale.German, ToTexts(form.De));
    experience.SetTexts(Locale.English, ToTexts(form.En));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes a station with its translations.</summary>
  /// <exception cref="DomainNotFoundException">No station has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var experience = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.Experiences.Remove(experience);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static IQueryable<Experience> Ordered(IQueryable<Experience> query)
    => query.OrderByDescending(experience => experience.StartDate).ThenBy(experience => experience.SortOrder);

  private async Task<Experience> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = dbContext.Experiences.Include(experience => experience.Translations);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Experience {id} does not exist.");
  }

  private static PublicExperience ToPublic(Experience experience, Locale locale)
  {
    var text = new LocalizedText<ExperienceTranslation>(experience.Translations, locale);
    var roleTitle = text.Text(t => t.RoleTitle, nameof(PublicExperience.RoleTitle))!;
    var summary = text.Text(t => t.Summary, nameof(PublicExperience.Summary));
    var bullets = text.List(t => t.Bullets, nameof(PublicExperience.Bullets));
    var stackLine = text.Text(t => t.StackLine, nameof(PublicExperience.StackLine));
    return new PublicExperience(
      experience.Id,
      experience.Company,
      experience.Location,
      experience.StartDate,
      experience.EndDate,
      roleTitle,
      summary,
      bullets,
      stackLine,
      text.FallbackFields);
  }

  private static AdminExperience ToAdmin(Experience experience)
    => new(
      experience.Id,
      experience.Company,
      experience.Location,
      experience.StartDate,
      experience.EndDate,
      experience.SortOrder,
      experience.IsVisible,
      ToForm(experience.Translations.Single(t => t.Locale == Locale.German)),
      experience.Translations.Where(t => t.Locale == Locale.English).Select(ToForm).SingleOrDefault());

  private static ExperienceDetails ToDetails(ExperienceForm form)
    => new(form.Company, form.Location, form.StartDate, form.EndDate, form.SortOrder, form.IsVisible);

  private static ExperienceTexts ToTexts(ExperienceTextsForm? form)
    => new(form?.RoleTitle, form?.Summary, form?.Bullets ?? [], form?.StackLine);

  private static ExperienceTextsForm ToForm(ExperienceTranslation translation)
    => new(translation.RoleTitle, translation.Summary, translation.Bullets, translation.StackLine);
}
