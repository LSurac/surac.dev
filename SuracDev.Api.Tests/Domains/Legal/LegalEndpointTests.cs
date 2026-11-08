using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Legal.Contracts;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Legal;

[Collection(PostgresCollection.Name)]
public sealed class LegalEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task The_imprint_carries_the_name_address_and_email_from_the_profile()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var page = (await client.GetFromJsonAsync<PublicLegalPage>("/api/public/legal/imprint", ApiJson.Options))!;

    Assert.Equal(LegalPageKey.Imprint, page.Key);
    Assert.Equal("Impressum", page.Title);
    Assert.StartsWith("<p>Platzhalter", page.BodyHtml, StringComparison.Ordinal);
    Assert.Equal("Max Mustermann", page.Contact!.FullName);
    Assert.Equal(["Musterstraße 1", "00000 Musterstadt"], page.Contact.AddressLines);
  }

  [PostgresFact]
  public async Task The_privacy_page_has_no_contact_block_and_comes_in_english()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var page = (await client.GetFromJsonAsync<PublicLegalPage>(
      "/api/public/legal/privacy?locale=en",
      ApiJson.Options))!;

    Assert.Equal("Privacy policy", page.Title);
    Assert.Null(page.Contact);
    Assert.Empty(page.FallbackFields);
  }

  [PostgresFact]
  public async Task An_unknown_page_answers_404_and_a_malformed_key_400()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/legal/unknown")).StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/public/legal/agb")).StatusCode);
  }

  [PostgresFact]
  public async Task The_admin_edits_one_locale_and_only_its_stamp_moves()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var before = (await admin.GetFromJsonAsync<AdminLegalPage>("/api/admin/legal-pages/privacy", ApiJson.Options))!;

    var put = await admin.PutAsJsonAsync(
      "/api/admin/legal-pages/privacy",
      new LegalPageForm(
        new LegalTextsForm(before.De.Title, "## Verantwortlicher\n\nText"),
        new LegalTextsForm(before.En!.Title, before.En.BodyMarkdown)));
    var after = (await admin.GetFromJsonAsync<AdminLegalPage>("/api/admin/legal-pages/privacy", ApiJson.Options))!;
    using var client = factory.CreateClient();
    var shown = (await client.GetFromJsonAsync<PublicLegalPage>("/api/public/legal/privacy", ApiJson.Options))!;

    Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
    Assert.True(after.De.UpdatedAt > before.De.UpdatedAt);
    Assert.Equal(before.En.UpdatedAt, after.En!.UpdatedAt);
    Assert.Equal("<h2>Verantwortlicher</h2>\n<p>Text</p>", shown.BodyHtml);
  }

  [PostgresFact]
  public async Task Removing_the_english_texts_makes_the_page_fall_back_to_german()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    await admin.PutAsJsonAsync(
      "/api/admin/legal-pages/imprint", new LegalPageForm(new LegalTextsForm("Impressum", "Text"), null));
    using var client = factory.CreateClient();
    var page = (await client.GetFromJsonAsync<PublicLegalPage>(
      "/api/public/legal/imprint?locale=en",
      ApiJson.Options))!;
    var all = (await admin.GetFromJsonAsync<List<AdminLegalPage>>("/api/admin/legal-pages", ApiJson.Options))!;

    Assert.Equal("Impressum", page.Title);
    Assert.Equal(["title", "bodyHtml"], page.FallbackFields);
    Assert.Equal([LegalPageKey.Imprint, LegalPageKey.Privacy], all.Select(p => p.Key));
    Assert.Null(all[0].En);
  }

  [PostgresFact]
  public async Task A_blank_german_body_answers_400()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await admin.PutAsJsonAsync(
      "/api/admin/legal-pages/imprint", new LegalPageForm(new LegalTextsForm("Impressum", " "), null));
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("de.bodyMarkdown", problem!.Errors.Keys);
  }
}
