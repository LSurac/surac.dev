using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Projects;

[Collection(PostgresCollection.Name)]
public sealed class ProjectEndpointTests(PostgresFixture postgres)
{
  private static ProjectForm NewProject(string slug = "neues-projekt") => new(
    Slug: slug,
    Status: ProjectStatus.InProduction,
    StartedYear: 2025,
    EndedYear: null,
    LiveUrl: null,
    RepositoryUrl: "https://github.com/example/repo",
    StackTags: [".NET", "Angular"],
    SortOrder: 1,
    IsFeatured: false,
    IsVisible: true,
    De: new ProjectTextsForm(
      "Neues Projekt",
      "Kurzbeschreibung",
      "Problem",
      "Rolle",
      "Ergebnis",
      "## Abschnitt\n\n**Text**"),
    En: new ProjectTextsForm("New project", null, null, null, null, null),
    Decisions: [new DecisionForm(new DecisionTextsForm("Entscheidung", "Abwägung"), null)],
    Images: []);

  [PostgresFact]
  public async Task The_public_list_shows_visible_projects_in_order_and_can_narrow_to_featured_ones()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/projects", NewProject());
      await admin.PostAsJsonAsync("/api/admin/projects", NewProject("versteckt") with { IsVisible = false });
    }

    using var client = factory.CreateClient();
    var all = await client.GetFromJsonAsync<List<PublicProjectCard>>("/api/public/projects", ApiJson.Options);
    var featured = await client.GetFromJsonAsync<List<PublicProjectCard>>(
      "/api/public/projects?featured=true", ApiJson.Options);

    Assert.Equal(["beispielprojekt", "neues-projekt"], all!.Select(project => project.Slug));
    Assert.Equal(["beispielprojekt"], featured!.Select(project => project.Slug));
    Assert.Equal(ProjectStatus.InProduction, all![1].Status);
  }

  [PostgresFact]
  public async Task A_case_study_comes_with_rendered_html_decisions_and_english_fallback()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/projects", NewProject());
    }

    using var client = factory.CreateClient();
    var study = await client.GetFromJsonAsync<PublicCaseStudy>(
      "/api/public/projects/neues-projekt?locale=en", ApiJson.Options);

    Assert.Equal("New project", study!.Title);
    Assert.Equal("Kurzbeschreibung", study.Teaser);
    Assert.Equal("<h2>Abschnitt</h2>\n<p><strong>Text</strong></p>", study.CaseStudyHtml);
    Assert.Equal("Entscheidung", Assert.Single(study.Decisions).Title);
    Assert.Contains("caseStudyHtml", study.FallbackFields);
    Assert.Contains("decisions", study.FallbackFields);
  }

  [PostgresFact]
  public async Task Hidden_unknown_and_malformed_slugs_answer_404()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/projects", NewProject("versteckt") with { IsVisible = false });
    }

    using var client = factory.CreateClient();

    Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/projects/versteckt")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/projects/gibt-es-nicht")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/projects/Kein%20Slug")).StatusCode);
  }

  [PostgresFact]
  public async Task The_admin_creates_reads_updates_and_deletes_a_project()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/projects", NewProject());
    var project = await created.Content.ReadFromJsonAsync<AdminProject>(ApiJson.Options);
    var updated = await admin.PutAsJsonAsync(
      created.Headers.Location, NewProject("umbenannt") with { Decisions = [], En = null });
    var read = await admin.GetFromJsonAsync<AdminProject>(created.Headers.Location, ApiJson.Options);
    var deleted = await admin.DeleteAsync(created.Headers.Location);
    var list = await admin.GetFromJsonAsync<List<AdminProject>>("/api/admin/projects", ApiJson.Options);

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Single(project!.Decisions);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal("umbenannt", read!.Slug);
    Assert.Empty(read.Decisions);
    Assert.Null(read.En);
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(["beispielprojekt"], list!.Select(p => p.Slug));
  }

  [PostgresFact]
  public async Task A_slug_already_in_use_is_a_conflict()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await admin.PostAsJsonAsync("/api/admin/projects", NewProject("beispielprojekt"));

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
  }

  [PostgresFact]
  public async Task Keeping_the_own_slug_on_update_is_not_a_conflict()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var created = await admin.PostAsJsonAsync("/api/admin/projects", NewProject());

    var updated = await admin.PutAsJsonAsync(created.Headers.Location, NewProject() with { SortOrder = 5 });

    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
  }

  [PostgresFact]
  public async Task At_most_five_visible_projects_can_be_featured()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    for (var i = 1; i <= 4; i++)
    {
      await admin.PostAsJsonAsync("/api/admin/projects", NewProject($"featured-{i}") with { IsFeatured = true });
    }

    var sixth = await admin.PostAsJsonAsync("/api/admin/projects", NewProject("featured-6") with { IsFeatured = true });
    var hidden = await admin.PostAsJsonAsync(
      "/api/admin/projects", NewProject("featured-hidden") with { IsFeatured = true, IsVisible = false });

    Assert.Equal(HttpStatusCode.Conflict, sixth.StatusCode);
    Assert.Equal(HttpStatusCode.Created, hidden.StatusCode);
  }

  [PostgresFact]
  public async Task Images_must_be_public_image_documents()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var screenshot = await factory.AddDocumentAsync(DocumentKind.Image);
    var certificate = await factory.AddDocumentAsync(
      DocumentKind.Certificate, DocumentVisibility.LinkOnly, DocumentContentTypes.Pdf);

    var rejected = await admin.PostAsJsonAsync(
      "/api/admin/projects", NewProject() with { Images = [new ImageForm(certificate, null, null)] });
    var problem = await rejected.Content.ReadFromJsonAsync<ValidationProblemDetails>();
    var accepted = await admin.PostAsJsonAsync(
      "/api/admin/projects", NewProject() with { Images = [new ImageForm(screenshot, "Bild", "Image")] });
    using var client = factory.CreateClient();
    var study = await client.GetFromJsonAsync<PublicCaseStudy>(
      "/api/public/projects/neues-projekt?locale=en", ApiJson.Options);

    Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    Assert.Contains("images", problem!.Errors.Keys);
    Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    Assert.Equal("Image", Assert.Single(study!.Images).Caption);
  }
}
