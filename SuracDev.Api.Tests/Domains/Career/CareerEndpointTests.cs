using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Career;

[Collection(PostgresCollection.Name)]
public sealed class CareerEndpointTests(PostgresFixture postgres)
{
  private static readonly ExperienceForm NewExperience = new(
    Company: "Neue GmbH",
    Location: "Beispielstadt",
    StartDate: new DateOnly(2024, 5, 1),
    EndDate: null,
    SortOrder: 0,
    IsVisible: true,
    De: new ExperienceTextsForm("Entwickler", "Zusammenfassung", ["Punkt"], ".NET"),
    En: new ExperienceTextsForm("Developer", null, [], null));

  private static readonly EducationForm NewEducation = new(
    Institution: "Hochschule",
    Location: null,
    StartDate: new DateOnly(2015, 10, 1),
    EndDate: new DateOnly(2018, 9, 30),
    Grade: "2,0",
    SortOrder: 0,
    IsVisible: true,
    De: new EducationTextsForm("Bachelor", null),
    En: null);

  [PostgresFact]
  public async Task The_public_career_lists_visible_stations_newest_first()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/experiences", NewExperience);
      await admin.PostAsJsonAsync("/api/admin/experiences", NewExperience with { Company = "Versteckt AG", IsVisible = false });
    }

    using var client = factory.CreateClient();
    var stations = await client.GetFromJsonAsync<List<PublicExperience>>("/api/public/experiences");

    Assert.Equal(["Neue GmbH", "Beispiel GmbH"], stations!.Select(station => station.Company));
    Assert.Equal(["Punkt"], stations![0].Bullets);
  }

  [PostgresFact]
  public async Task English_stations_fall_back_to_german_per_field()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/experiences", NewExperience);
    }

    using var client = factory.CreateClient();
    var stations = await client.GetFromJsonAsync<List<PublicExperience>>("/api/public/experiences?locale=en");
    var station = stations!.Single(s => s.Company == "Neue GmbH");

    Assert.Equal("Developer", station.RoleTitle);
    Assert.Equal("Zusammenfassung", station.Summary);
    Assert.Equal(["summary", "bullets", "stackLine"], station.FallbackFields);
  }

  [PostgresFact]
  public async Task The_admin_creates_reads_updates_and_deletes_a_station()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/experiences", NewExperience);
    var station = await created.Content.ReadFromJsonAsync<AdminExperience>();
    var location = created.Headers.Location!;
    var updated = await admin.PutAsJsonAsync(location, NewExperience with { Company = "Umbenannt GmbH", En = null });
    var read = await admin.GetFromJsonAsync<AdminExperience>(location);
    var deleted = await admin.DeleteAsync(location);
    var gone = await admin.GetAsync(location);

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Equal($"/api/admin/experiences/{station!.Id}", location.AbsolutePath);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal("Umbenannt GmbH", read!.Company);
    Assert.Null(read.En);
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
  }

  [PostgresFact]
  public async Task The_admin_list_includes_hidden_stations()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    await admin.PostAsJsonAsync("/api/admin/experiences", NewExperience with { IsVisible = false });

    var all = await admin.GetFromJsonAsync<List<AdminExperience>>("/api/admin/experiences");

    Assert.Equal(2, all!.Count);
    Assert.Contains(all, station => !station.IsVisible);
  }

  [PostgresFact]
  public async Task Unknown_ids_answer_404()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var put = await admin.PutAsJsonAsync("/api/admin/experiences/999999", NewExperience);
    var delete = await admin.DeleteAsync("/api/admin/education/999999");

    Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
  }

  [PostgresFact]
  public async Task An_end_before_the_start_answers_400_on_the_end_date()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await admin.PostAsJsonAsync(
      "/api/admin/experiences", NewExperience with { EndDate = new DateOnly(2024, 4, 1) });
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("endDate", problem!.Errors.Keys);
  }

  [PostgresFact]
  public async Task Education_is_listed_publicly_and_managed_by_the_admin()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/education", NewEducation);
    var updated = await admin.PutAsJsonAsync(created.Headers.Location, NewEducation with { Grade = "1,7" });
    using var client = factory.CreateClient();
    var entries = await client.GetFromJsonAsync<List<PublicEducation>>("/api/public/education?locale=en");
    var all = await admin.GetFromJsonAsync<List<AdminEducation>>("/api/admin/education");

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal(["Beispiel-Berufsschule", "Hochschule"], entries!.Select(entry => entry.Institution));
    Assert.Equal("1,7", entries![1].Grade);
    Assert.Equal(["degree"], entries[1].FallbackFields);
    Assert.Equal(2, all!.Count);
  }
}
