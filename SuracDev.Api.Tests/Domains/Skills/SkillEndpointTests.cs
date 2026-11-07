using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Skills.Contracts;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Skills;

[Collection(PostgresCollection.Name)]
public sealed class SkillEndpointTests(PostgresFixture postgres)
{
  private static readonly SkillGroupForm Operations = new(
    SortOrder: 2,
    NameDe: "Betrieb",
    NameEn: null,
    Skills:
    [
      new SkillForm("Docker", 2022, null, null),
      new SkillForm("VB.NET", 2019, "Legacy", "legacy"),
    ]);

  [PostgresFact]
  public async Task The_public_skills_come_grouped_in_order_with_since_year_and_qualifier()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/skill-groups", Operations);
    }

    using var client = factory.CreateClient();
    var groups = (await client.GetFromJsonAsync<List<PublicSkillGroup>>("/api/public/skills?locale=en"))!;

    Assert.Equal(["Backend", "Frontend", "Betrieb"], groups.Select(group => group.Name));
    Assert.Equal(["C#", "ASP.NET Core", "PostgreSQL"], groups[0].Skills.Select(skill => skill.Name));
    var operations = groups[2];
    Assert.Equal(["name"], operations.FallbackFields);
    Assert.Equal(2022, operations.Skills[0].SinceYear);
    Assert.Equal("legacy", operations.Skills[1].Qualifier);
  }

  [PostgresFact]
  public async Task The_admin_creates_reads_replaces_and_deletes_a_group_with_its_skills()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/skill-groups", Operations);
    var updated = await admin.PutAsJsonAsync(
      created.Headers.Location, Operations with { NameEn = "Operations", Skills = [new SkillForm("Linux", 2020, null, null)] });
    var read = await admin.GetFromJsonAsync<AdminSkillGroup>(created.Headers.Location);
    var deleted = await admin.DeleteAsync(created.Headers.Location);
    var all = await admin.GetFromJsonAsync<List<AdminSkillGroup>>("/api/admin/skill-groups");

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal("Operations", read!.NameEn);
    Assert.Equal(["Linux"], read.Skills.Select(skill => skill.Name));
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(2, all!.Count);
  }

  [PostgresFact]
  public async Task A_duplicate_skill_name_in_a_group_answers_400()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await admin.PostAsJsonAsync(
      "/api/admin/skill-groups",
      Operations with { Skills = [new SkillForm("Docker", null, null, null), new SkillForm("docker", null, null, null)] });
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("skills", problem!.Errors.Keys);
  }

  [PostgresFact]
  public async Task Unknown_groups_answer_404()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/skill-groups/999999")).StatusCode);
    Assert.Equal(
      HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/admin/skill-groups/999999", Operations)).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/admin/skill-groups/999999")).StatusCode);
  }
}
