using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Career.Application;

/// <summary>The use cases of education entries: the public list and the admin's CRUD.</summary>
public sealed class EducationService(SuracDevDbContext dbContext)
{
  /// <summary>Visible entries, newest first, in <paramref name="locale"/> with German fallback.</summary>
  public async Task<IReadOnlyList<PublicEducation>> ListPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var entries = await Ordered(dbContext.Educations.AsNoTracking().Include(e => e.Translations))
      .Where(education => education.IsVisible)
      .ToListAsync(cancellationToken);

    return entries.Select(education => ToPublic(education, locale)).ToList();
  }

  /// <summary>Every entry including hidden ones, newest first.</summary>
  public async Task<IReadOnlyList<AdminEducation>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var entries = await Ordered(dbContext.Educations.AsNoTracking().Include(e => e.Translations))
      .ToListAsync(cancellationToken);
    return entries.Select(ToAdmin).ToList();
  }

  /// <summary>One entry for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No entry has this id.</exception>
  public async Task<AdminEducation> GetAdminAsync(long id, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates an entry and returns it as stored.</summary>
  /// <exception cref="DomainValidationException">A field breaks an education rule.</exception>
  public async Task<AdminEducation> CreateAsync(EducationForm form, CancellationToken cancellationToken)
  {
    var education = Education.Create(ToDetails(form), ToTexts(form.De));
    education.SetTexts(Locale.English, ToTexts(form.En));
    dbContext.Educations.Add(education);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ToAdmin(education);
  }

  /// <summary>Replaces an entry with the given fields.</summary>
  /// <exception cref="DomainNotFoundException">No entry has this id.</exception>
  /// <exception cref="DomainValidationException">A field breaks an education rule.</exception>
  public async Task UpdateAsync(long id, EducationForm form, CancellationToken cancellationToken)
  {
    var education = await FindAsync(id, tracked: true, cancellationToken);
    education.UpdateDetails(ToDetails(form));
    education.SetTexts(Locale.German, ToTexts(form.De));
    education.SetTexts(Locale.English, ToTexts(form.En));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes an entry with its translations.</summary>
  /// <exception cref="DomainNotFoundException">No entry has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var education = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.Educations.Remove(education);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static IQueryable<Education> Ordered(IQueryable<Education> query)
    => query.OrderByDescending(education => education.StartDate).ThenBy(education => education.SortOrder);

  private async Task<Education> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = dbContext.Educations.Include(education => education.Translations);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Education entry {id} does not exist.");
  }

  private static PublicEducation ToPublic(Education education, Locale locale)
  {
    var text = new LocalizedText<EducationTranslation>(education.Translations, locale);
    var degree = text.Text(t => t.Degree, nameof(PublicEducation.Degree))!;
    var summary = text.Text(t => t.Summary, nameof(PublicEducation.Summary));
    return new PublicEducation(
      education.Id,
      education.Institution,
      education.Location,
      education.StartDate,
      education.EndDate,
      education.Grade,
      degree,
      summary,
      text.FallbackFields);
  }

  private static AdminEducation ToAdmin(Education education)
    => new(
      education.Id,
      education.Institution,
      education.Location,
      education.StartDate,
      education.EndDate,
      education.Grade,
      education.SortOrder,
      education.IsVisible,
      ToForm(education.Translations.Single(t => t.Locale == Locale.German)),
      education.Translations.Where(t => t.Locale == Locale.English).Select(ToForm).SingleOrDefault());

  private static EducationDetails ToDetails(EducationForm form)
    => new(form.Institution, form.Location, form.StartDate, form.EndDate, form.Grade, form.SortOrder, form.IsVisible);

  private static EducationTexts ToTexts(EducationTextsForm? form) => new(form?.Degree, form?.Summary);

  private static EducationTextsForm ToForm(EducationTranslation translation)
    => new(translation.Degree, translation.Summary);
}
