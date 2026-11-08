using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Projects.Application;

/// <summary>
/// The use cases of projects: cards and case studies for the open site, CRUD for the admin. Owns
/// the rules that span projects: a unique slug and at most five featured visible projects.
/// </summary>
public sealed class ProjectService(SuracDevDbContext dbContext, DocumentReferences documentReferences)
{
  /// <summary>How many visible projects the start page features.</summary>
  public const int MaxFeatured = 5;

  /// <summary>Visible project cards in display order; only the featured ones when <paramref name="featuredOnly"/> is
  /// set.</summary>
  public async Task<IReadOnlyList<PublicProjectCard>> ListPublicAsync(
    Locale locale,
    bool featuredOnly,
    CancellationToken cancellationToken)
  {
    var query = Ordered(dbContext.Projects.AsNoTracking()
      .Include(project => project.Translations)
      .Where(project => project.IsVisible));
    if (featuredOnly)
    {
      query = query.Where(project => project.IsFeatured).Take(MaxFeatured);
    }

    var projects = await query.ToListAsync(cancellationToken);
    return projects.Select(project => ProjectMapping.ToCard(project, locale)).ToList();
  }

  /// <summary>The case study of a visible project.</summary>
  /// <exception cref="DomainNotFoundException">No visible project has this slug, or the slug is malformed.</exception>
  public async Task<PublicCaseStudy> GetCaseStudyAsync(string slug, Locale locale, CancellationToken cancellationToken)
  {
    var notFound = new DomainNotFoundException($"No project '{slug}'.");
    if (!Slug.TryParse(slug, out var parsed))
    {
      throw notFound;
    }

    var project = await Complete(dbContext.Projects.AsNoTracking())
      .SingleOrDefaultAsync(p => p.Slug == parsed && p.IsVisible, cancellationToken) ?? throw notFound;
    return ProjectMapping.ToCaseStudy(project, locale);
  }

  /// <summary>Every project including hidden ones, in display order.</summary>
  public async Task<IReadOnlyList<AdminProject>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var projects = await Ordered(Complete(dbContext.Projects.AsNoTracking())).ToListAsync(cancellationToken);
    return projects.Select(ProjectMapping.ToAdmin).ToList();
  }

  /// <summary>One project for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No project has this id.</exception>
  public async Task<AdminProject> GetAdminAsync(long id, CancellationToken cancellationToken)
    => ProjectMapping.ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates a project and returns it as stored.</summary>
  /// <exception cref="DomainValidationException">A field breaks a project rule or an image is not a public
  /// image.</exception>
  /// <exception cref="DomainConflictException">The slug is taken or a sixth project would be featured.</exception>
  public async Task<AdminProject> CreateAsync(ProjectForm form, CancellationToken cancellationToken)
  {
    await EnsureAllowedAsync(form, ownId: 0, cancellationToken);
    var project = Project.Create(ProjectMapping.ToDetails(form), ProjectMapping.ToTexts(form.De));
    Apply(project, form);
    dbContext.Projects.Add(project);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ProjectMapping.ToAdmin(project);
  }

  /// <summary>Replaces a project, its decisions and its images.</summary>
  /// <exception cref="DomainNotFoundException">No project has this id.</exception>
  /// <exception cref="DomainValidationException">A field breaks a project rule or an image is not a public
  /// image.</exception>
  /// <exception cref="DomainConflictException">The slug is taken or a sixth project would be featured.</exception>
  public async Task UpdateAsync(long id, ProjectForm form, CancellationToken cancellationToken)
  {
    var project = await FindAsync(id, tracked: true, cancellationToken);
    await EnsureAllowedAsync(form, id, cancellationToken);
    project.UpdateDetails(ProjectMapping.ToDetails(form));
    project.SetTexts(Locale.German, ProjectMapping.ToTexts(form.De));
    Apply(project, form);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes a project with its texts, decisions and image references; the documents stay.</summary>
  /// <exception cref="DomainNotFoundException">No project has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var project = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.Projects.Remove(project);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static void Apply(Project project, ProjectForm form)
  {
    project.SetTexts(Locale.English, ProjectMapping.ToTexts(form.En));
    project.ReplaceDecisions(ProjectMapping.ToDecisions(form));
    project.ReplaceImages(ProjectMapping.ToImages(form));
  }

  private async Task EnsureAllowedAsync(ProjectForm form, long ownId, CancellationToken cancellationToken)
  {
    var slug = Slug.Parse(form.Slug);
    await documentReferences.EnsurePublicImagesAsync(
      form.Images.Select(image => image.DocumentId).ToList(), "images", cancellationToken);

    if (await dbContext.Projects.AnyAsync(project => project.Slug == slug && project.Id != ownId, cancellationToken))
    {
      throw new DomainConflictException($"The slug '{slug}' is already used by another project.");
    }

    if (!form.IsFeatured || !form.IsVisible)
    {
      return;
    }

    var featured = await dbContext.Projects.CountAsync(
      project => project.IsFeatured && project.IsVisible && project.Id != ownId, cancellationToken);
    if (featured >= MaxFeatured)
    {
      throw new DomainConflictException($"At most {MaxFeatured} visible projects can be featured.");
    }
  }

  private async Task<Project> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = Complete(dbContext.Projects);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Project {id} does not exist.");
  }

  private static IQueryable<Project> Complete(IQueryable<Project> query)
    => query
      .Include(project => project.Translations)
      .Include(project => project.Decisions.OrderBy(decision => decision.SortOrder))
      .ThenInclude(decision => decision.Translations)
      .Include(project => project.Images.OrderBy(image => image.SortOrder))
      .ThenInclude(image => image.Translations)
      .AsSplitQuery();

  private static IQueryable<Project> Ordered(IQueryable<Project> query)
    => query.OrderBy(project => project.SortOrder).ThenBy(project => project.Id);
}
