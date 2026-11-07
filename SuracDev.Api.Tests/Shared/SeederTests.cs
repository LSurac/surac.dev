using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure.Seeding;

namespace SuracDev.Api.Tests.Shared;

[Collection(PostgresCollection.Name)]
public sealed class SeederTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task A_fresh_database_gets_neutral_example_content_in_both_locales()
  {
    await using var dbContext = await SeededContextAsync();

    var profile = await dbContext.Profiles.Include(p => p.Translations).Include(p => p.Facts).SingleAsync();
    Assert.Contains("Mustermann", profile.FullName, StringComparison.Ordinal);
    Assert.EndsWith("@example.org", profile.Email, StringComparison.Ordinal);
    Assert.Equal(2, profile.Translations.Count);
    Assert.Equal(4, profile.Facts.Count);
    Assert.Single(await dbContext.Experiences.ToListAsync());
    Assert.Single(await dbContext.Educations.ToListAsync());
    Assert.Single(await dbContext.Projects.ToListAsync());
    Assert.Equal(2, await dbContext.SkillGroups.CountAsync());
    Assert.Equal(
      [LegalPageKey.Imprint, LegalPageKey.Privacy],
      await dbContext.LegalPages.OrderBy(p => p.Key).Select(p => p.Key).ToListAsync());
  }

  [PostgresFact]
  public async Task Every_seeded_text_says_it_is_an_example()
  {
    await using var dbContext = await SeededContextAsync();

    var headlines = await dbContext.Profiles.SelectMany(p => p.Translations).Select(t => t.Headline).ToListAsync();

    Assert.All(headlines, headline => Assert.Matches("(Beispiel|Example)", headline!));
  }

  [PostgresFact]
  public async Task Seeding_twice_changes_nothing()
  {
    await using var dbContext = await SeededContextAsync();

    await new SuracDevSeeder(dbContext, TimeProvider.System).SeedAsync();

    Assert.Single(await dbContext.Profiles.ToListAsync());
    Assert.Equal(2, await dbContext.LegalPages.CountAsync());
    Assert.Single(await dbContext.AdminUsers.ToListAsync());
  }

  [PostgresFact]
  public async Task Seeding_never_touches_content_that_already_exists()
  {
    await using var dbContext = await SeededContextAsync();
    var page = await dbContext.LegalPages.Include(p => p.Translations).SingleAsync(p => p.Key == LegalPageKey.Imprint);
    page.SetTexts(Locale.German, new LegalTexts("Eigenes Impressum", "Eigener Text"), DateTimeOffset.UtcNow);
    await dbContext.SaveChangesAsync();

    await new SuracDevSeeder(dbContext, TimeProvider.System).SeedAsync();

    var reloaded = await dbContext.LegalPages.AsNoTracking().Include(p => p.Translations)
      .SingleAsync(p => p.Key == LegalPageKey.Imprint);
    Assert.Equal("Eigenes Impressum", reloaded.Translations.Single(t => t.Locale == Locale.German).Title);
  }

  private async Task<SuracDevDbContext> SeededContextAsync()
  {
    var options = new DbContextOptionsBuilder<SuracDevDbContext>().UseNpgsql(await postgres.CreateDatabaseAsync()).Options;
    var dbContext = new SuracDevDbContext(options);
    await new SuracDevSeeder(dbContext, TimeProvider.System).SeedAsync();
    return dbContext;
  }
}
