using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Domains.Identity.Domain;

namespace SuracDev.Api.Data;

/// <summary>
/// EF Core context for the surac.dev database. Tables are snake_case, entity properties keep their
/// PascalCase names (quoted columns). All mapping is configured inline here (docs/architecture.md).
/// </summary>
public sealed class SuracDevDbContext(DbContextOptions<SuracDevDbContext> options) : DbContext(options)
{
  public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<AdminUser>(entity =>
    {
      entity.ToTable("admin_users");
      entity.HasKey(user => user.Id);
      entity.Property(user => user.Id).UseIdentityByDefaultColumn();
      entity.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
      entity.Property(user => user.PasswordHash).HasMaxLength(500);
      entity.Property(user => user.SecurityStamp).IsRequired();
      entity.Property(user => user.CreatedAt).IsRequired();
    });
  }
}
