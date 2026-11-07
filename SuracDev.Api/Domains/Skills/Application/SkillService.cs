using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Skills.Contracts;
using SuracDev.Api.Domains.Skills.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Skills.Application;

/// <summary>The use cases of skill groups: the public skills section and the admin's CRUD.</summary>
public sealed class SkillService(SuracDevDbContext dbContext)
{
  /// <summary>Every group with its skills, in display order, in <paramref name="locale"/> with German fallback.</summary>
  public async Task<IReadOnlyList<PublicSkillGroup>> ListPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var groups = await Complete(dbContext.SkillGroups.AsNoTracking()).ToListAsync(cancellationToken);
    return groups.Select(group => ToPublic(group, locale)).ToList();
  }

  /// <summary>Every group with both names and its skills, in display order.</summary>
  public async Task<IReadOnlyList<AdminSkillGroup>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var groups = await Complete(dbContext.SkillGroups.AsNoTracking()).ToListAsync(cancellationToken);
    return groups.Select(ToAdmin).ToList();
  }

  /// <summary>One group for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No group has this id.</exception>
  public async Task<AdminSkillGroup> GetAdminAsync(long id, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates a group with its skills and returns it as stored.</summary>
  /// <exception cref="DomainValidationException">A name is missing, a year is implausible or a skill appears twice.</exception>
  public async Task<AdminSkillGroup> CreateAsync(SkillGroupForm form, CancellationToken cancellationToken)
  {
    var group = SkillGroup.Create(form.SortOrder, form.NameDe, form.NameEn);
    group.ReplaceSkills(ToInput(form));
    dbContext.SkillGroups.Add(group);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ToAdmin(group);
  }

  /// <summary>Replaces a group's names, position and skills.</summary>
  /// <exception cref="DomainNotFoundException">No group has this id.</exception>
  /// <exception cref="DomainValidationException">A name is missing, a year is implausible or a skill appears twice.</exception>
  public async Task UpdateAsync(long id, SkillGroupForm form, CancellationToken cancellationToken)
  {
    var group = await FindAsync(id, tracked: true, cancellationToken);
    group.MoveTo(form.SortOrder);
    group.Rename(form.NameDe, form.NameEn);
    group.ReplaceSkills(ToInput(form));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes a group with all its skills.</summary>
  /// <exception cref="DomainNotFoundException">No group has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var group = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.SkillGroups.Remove(group);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<SkillGroup> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = Complete(dbContext.SkillGroups);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(g => g.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Skill group {id} does not exist.");
  }

  private static IQueryable<SkillGroup> Complete(IQueryable<SkillGroup> query)
    => query
      .Include(group => group.Translations)
      .Include(group => group.Skills.OrderBy(skill => skill.SortOrder)).ThenInclude(skill => skill.Translations)
      .AsSplitQuery()
      .OrderBy(group => group.SortOrder)
      .ThenBy(group => group.Id);

  private static PublicSkillGroup ToPublic(SkillGroup group, Locale locale)
  {
    var text = new LocalizedText<SkillGroupTranslation>(group.Translations, locale);
    var name = text.Text(t => t.Name, nameof(PublicSkillGroup.Name))!;
    var skills = group.Skills.Select(skill => new PublicSkill(
        skill.Name,
        skill.SinceYear,
        new LocalizedText<SkillTranslation>(skill.Translations, locale).Text(t => t.Qualifier, nameof(PublicSkill.Qualifier))))
      .ToList();
    return new PublicSkillGroup(group.Id, name, skills, text.FallbackFields);
  }

  private static AdminSkillGroup ToAdmin(SkillGroup group)
    => new(
      group.Id,
      group.SortOrder,
      group.Translations.Single(t => t.Locale == Locale.German).Name!,
      group.Translations.SingleOrDefault(t => t.Locale == Locale.English)?.Name,
      group.Skills.Select(skill => new SkillForm(
          skill.Name,
          skill.SinceYear,
          skill.Translations.Single(t => t.Locale == Locale.German).Qualifier,
          skill.Translations.SingleOrDefault(t => t.Locale == Locale.English)?.Qualifier))
        .ToList());

  private static IReadOnlyList<SkillInput> ToInput(SkillGroupForm form)
    => form.Skills.Select(skill => new SkillInput(skill.Name, skill.SinceYear, skill.QualifierDe, skill.QualifierEn)).ToList();
}
