using Microsoft.EntityFrameworkCore;
using Npgsql;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Interests.Domain;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Domains.Profiles.Domain;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Domains.Skills.Domain;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Shared;

/// <summary>The EF model, the migrations and PostgreSQL agree: every aggregate survives a round trip.</summary>
[Collection(PostgresCollection.Name)]
public sealed class SchemaTests(PostgresFixture postgres)
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  [PostgresFact]
  public async Task The_model_has_no_changes_missing_from_the_migrations()
  {
    await using var dbContext = await NewContextAsync();

    Assert.False(dbContext.Database.HasPendingModelChanges());
  }

  [PostgresFact]
  public async Task A_profile_round_trips_with_facts_and_both_locales()
  {
    var connectionString = await postgres.CreateDatabaseAsync();
    await using (var write = Context(connectionString))
    {
      var document = NewDocument(DocumentKind.Photo, DocumentVisibility.Public, "image/png");
      write.Documents.Add(document);
      await write.SaveChangesAsync();

      var profile = Profile.Create(
        new ProfileContact("Max Mustermann", "max@example.org", null, "Musterstadt", ["Musterstraße 1"], null, null),
        new ProfileTexts("Titel", "Überschrift", null, null));
      profile.SetTexts(Locale.English, new ProfileTexts("Title", null, null, null));
      profile.ReplaceFacts([new FactInput("5", "Jahre", "Years")]);
      profile.SetPhoto(document.Id);
      write.Profiles.Add(profile);
      await write.SaveChangesAsync();
    }

    await using var read = Context(connectionString);
    var loaded = await read.Profiles
      .Include(p => p.Translations)
      .Include(p => p.Facts).ThenInclude(f => f.Translations)
      .SingleAsync();

    Assert.Equal(["Musterstraße 1"], loaded.AddressLines);
    Assert.Equal(2, loaded.Translations.Count);
    Assert.Equal(2, Assert.Single(loaded.Facts).Translations.Count);
    Assert.NotNull(loaded.PhotoDocumentId);
  }

  [PostgresFact]
  public async Task Career_projects_skills_and_legal_pages_round_trip()
  {
    var connectionString = await postgres.CreateDatabaseAsync();
    await using (var write = Context(connectionString))
    {
      var image = NewDocument(DocumentKind.Image, DocumentVisibility.Public, "image/webp");
      write.Documents.Add(image);
      await write.SaveChangesAsync();

      write.Experiences.Add(Experience.Create(
        new ExperienceDetails("Beispiel GmbH", null, new DateOnly(2022, 9, 1), null, 0, true),
        new ExperienceTexts("Entwickler", null, ["Eins", "Zwei"], null)));
      write.Educations.Add(Education.Create(
        new EducationDetails("Schule", null, new DateOnly(2019, 9, 1), new DateOnly(2022, 6, 30), "1,8", 0, true),
        new EducationTexts("Abschluss", null)));

      var project = Project.Create(
        new ProjectDetails("beispiel", ProjectStatus.Live, 2023, null, null, null, [".NET"], 0, true, true),
        new ProjectTexts("Beispiel", "Teaser", null, null, null, null));
      project.ReplaceDecisions([new DecisionInput(new DecisionTexts("Entscheidung", "Abwägung"), null)]);
      project.ReplaceImages([new ImageInput(image.Id, "Bild", "Image")]);
      write.Projects.Add(project);

      var group = SkillGroup.Create(0, "Backend", "Backend");
      group.ReplaceSkills([new SkillInput("C#", 2019, null, null), new SkillInput("VB.NET", 2019, "Legacy", null)]);
      write.SkillGroups.Add(group);

      write.LegalPages.Add(LegalPage.Create(LegalPageKey.Imprint, new LegalTexts("Impressum", "Text"), Now));
      await write.SaveChangesAsync();
    }

    await using var read = Context(connectionString);
    var experience = await read.Experiences.Include(e => e.Translations).SingleAsync();
    var loadedProject = await read.Projects
      .Include(p => p.Translations)
      .Include(p => p.Decisions).ThenInclude(d => d.Translations)
      .Include(p => p.Images).ThenInclude(i => i.Translations)
      .SingleAsync(p => p.Slug == Slug.Parse("beispiel"));
    var loadedGroup = await read.SkillGroups.Include(g => g.Skills).ThenInclude(s => s.Translations).SingleAsync();
    var page = await read.LegalPages.Include(p => p.Translations).SingleAsync(p => p.Key == LegalPageKey.Imprint);

    Assert.Equal(["Eins", "Zwei"], Assert.Single(experience.Translations).Bullets);
    Assert.Equal(ProjectStatus.Live, loadedProject.Status);
    Assert.Single(loadedProject.Decisions);
    Assert.Equal(2, Assert.Single(loadedProject.Images).Translations.Count);
    Assert.Equal(2, loadedGroup.Skills.Count);
    Assert.Equal(Now, Assert.Single(page.Translations).UpdatedAt);
    Assert.Single(await read.Educations.ToListAsync());
  }

  [PostgresFact]
  public async Task Interests_applications_links_and_events_round_trip()
  {
    var connectionString = await postgres.CreateDatabaseAsync();
    long linkId;
    var token = ApplicationLinkToken.Generate();
    await using (var write = Context(connectionString))
    {
      var certificate = NewDocument(DocumentKind.Certificate, DocumentVisibility.LinkOnly, "application/pdf");
      var application = Application.Create(
        new ApplicationDetails("Beispiel AG", "Entwickler", null, null, ApplicationStatus.Draft, null), Now);
      write.AddRange(certificate, application);
      write.Interests.Add(Interest.Create(new InterestDetails(InterestKind.Chess, 0, true, null), new InterestTexts("Schach", null)));
      await write.SaveChangesAsync();

      var link = ApplicationLink.Create(
        application.Id, new LinkSettings(AudienceProfile.TechLead, null, null, Now.AddDays(90), true), token.Hash, Now);
      write.ApplicationLinks.Add(link);
      await write.SaveChangesAsync();
      link.AttachDocuments([certificate]);
      write.LinkEvents.Add(LinkEvent.Create(link.Id, LinkEventKind.DocumentDownloaded, Locale.English, certificate.Id, Now));
      await write.SaveChangesAsync();
      linkId = link.Id;
    }

    await using var read = Context(connectionString);
    var loadedLink = await read.ApplicationLinks.Include(l => l.Documents).SingleAsync(l => l.TokenHash == token.Hash);
    var linkEvent = await read.LinkEvents.SingleAsync();

    Assert.Equal(linkId, loadedLink.Id);
    Assert.Equal(AudienceProfile.TechLead, loadedLink.Profile);
    Assert.Single(loadedLink.Documents);
    Assert.Equal(Locale.English, linkEvent.Locale);
    Assert.Single(await read.Interests.ToListAsync());
  }

  [PostgresFact]
  public async Task The_database_refuses_a_locale_outside_de_and_en()
  {
    await using var dbContext = await NewContextAsync();

    var insert = () => dbContext.Database.ExecuteSqlRawAsync(
      "INSERT INTO skill_groups (\"SortOrder\") VALUES (0); "
      + "INSERT INTO skill_group_translations (\"SkillGroupId\", \"Locale\", \"Name\") "
      + "SELECT \"Id\", 'fr', 'x' FROM skill_groups");

    var exception = await Assert.ThrowsAsync<PostgresException>(insert);
    Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
  }

  private async Task<SuracDevDbContext> NewContextAsync() => Context(await postgres.CreateDatabaseAsync());

  private static SuracDevDbContext Context(string connectionString)
    => new(new DbContextOptionsBuilder<SuracDevDbContext>().UseNpgsql(connectionString).Options);

  private static Document NewDocument(DocumentKind kind, DocumentVisibility visibility, string contentType)
    => Document.Create(
      new DocumentUpload("datei", contentType, 10, $"x/{Guid.NewGuid():N}", new string('a', 64), kind, visibility, "Label", null),
      Now);
}
