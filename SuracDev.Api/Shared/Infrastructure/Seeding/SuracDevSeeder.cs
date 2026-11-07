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
  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    await SeedAdminAsync(cancellationToken);
  }

  private async Task SeedAdminAsync(CancellationToken cancellationToken)
  {
    if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
    {
      return;
    }

    // The first login sets the password.
    dbContext.AdminUsers.Add(AdminUser.Create("Admin", timeProvider.GetUtcNow()));
    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
