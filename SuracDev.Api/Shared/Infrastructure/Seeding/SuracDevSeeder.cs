using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Identity.Domain;

namespace SuracDev.Api.Shared.Infrastructure.Seeding;

/// <summary>
/// Idempotent starter data: only fills empty tables, never changes existing rows. Runs at startup
/// after the migrations. Real content is never seeded; the repository is public
/// (docs/content-model.md, "Seed").
/// </summary>
public sealed class SuracDevSeeder(SuracDevDbContext dbContext, TimeProvider timeProvider)
{
  /// <summary>Adds the admin without a password and the example content to every table that is still empty.</summary>
  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    var now = timeProvider.GetUtcNow();

    // The first login sets the password.
    await SeedWhenEmptyAsync(dbContext.AdminUsers, () => [AdminUser.Create("Admin", now)], cancellationToken);
    await SeedWhenEmptyAsync(dbContext.Profiles, () => [ExampleContent.Profile()], cancellationToken);
    await SeedWhenEmptyAsync(dbContext.Experiences, () => [ExampleContent.Experience()], cancellationToken);
    await SeedWhenEmptyAsync(dbContext.Educations, () => [ExampleContent.Education()], cancellationToken);
    await SeedWhenEmptyAsync(dbContext.Projects, () => [ExampleContent.Project()], cancellationToken);
    await SeedWhenEmptyAsync(dbContext.SkillGroups, ExampleContent.SkillGroups, cancellationToken);
    await SeedWhenEmptyAsync(dbContext.LegalPages, () => ExampleContent.LegalPages(now), cancellationToken);
  }

  private async Task SeedWhenEmptyAsync<TEntity>(
    DbSet<TEntity> table,
    Func<IEnumerable<TEntity>> create,
    CancellationToken cancellationToken)
    where TEntity : class
  {
    if (await table.AnyAsync(cancellationToken))
    {
      return;
    }

    table.AddRange(create());
    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
