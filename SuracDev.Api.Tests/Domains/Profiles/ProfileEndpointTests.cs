using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Profiles;

[Collection(PostgresCollection.Name)]
public sealed class ProfileEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task The_public_profile_is_german_by_default_and_never_contains_phone_or_address()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      var current = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");
      await admin.PutAsJsonAsync("/api/admin/profile", current! with { Phone = "+49 000 111" });
    }

    var response = await client.GetAsync("/api/public/profile");
    var json = await response.Content.ReadAsStringAsync();
    var profile = await response.Content.ReadFromJsonAsync<PublicProfile>();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("Fullstack-Entwickler · .NET & Angular", profile!.TitleLine);
    Assert.Equal(4, profile.Facts.Count);
    Assert.Empty(profile.FallbackFields);
    Assert.DoesNotContain("+49 000 111", json, StringComparison.Ordinal);
    Assert.DoesNotContain("Musterstraße", json, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task English_falls_back_to_german_per_field_and_says_which_fields_did()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      var current = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");
      var put = await admin.PutAsJsonAsync(
        "/api/admin/profile",
        current! with { En = current.En! with { Intro = null }, Facts = [new FactForm("5", "Jahre", null)] });
      Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
    }

    using var client = factory.CreateClient();
    var profile = await client.GetFromJsonAsync<PublicProfile>("/api/public/profile?locale=en");

    Assert.Equal("Full-stack developer · .NET & Angular", profile!.TitleLine);
    Assert.StartsWith("Dieser Text", profile.Intro, StringComparison.Ordinal);
    Assert.Equal("Jahre", Assert.Single(profile.Facts).Label);
    Assert.Equal(["intro", "facts"], profile.FallbackFields);
  }

  [PostgresFact]
  public async Task An_unsupported_locale_is_a_validation_error()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/public/profile?locale=fr");
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("locale", problem!.Errors.Keys);
  }

  [PostgresFact]
  public async Task The_admin_reads_both_locales_side_by_side_with_the_private_contact_data()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var profile = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");

    Assert.Equal("Fullstack-Entwickler · .NET & Angular", profile!.De.TitleLine);
    Assert.Equal("Full-stack developer · .NET & Angular", profile.En!.TitleLine);
    Assert.Equal(["Musterstraße 1", "00000 Musterstadt"], profile.AddressLines);
  }

  [PostgresFact]
  public async Task Saving_replaces_the_profile_and_the_public_page_shows_it()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var current = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");

    var put = await admin.PutAsJsonAsync(
      "/api/admin/profile",
      current! with { FullName = "Erika Musterfrau", De = current.De with { Headline = "Neue Überschrift." }, En = null });
    using var client = factory.CreateClient();
    var german = await client.GetFromJsonAsync<PublicProfile>("/api/public/profile");
    var saved = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");

    Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
    Assert.Equal("Erika Musterfrau", german!.FullName);
    Assert.Equal("Neue Überschrift.", german.Headline);
    Assert.Null(saved!.En);
  }

  [PostgresFact]
  public async Task Invalid_input_answers_400_with_the_field_that_failed()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var current = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");

    var response = await admin.PutAsJsonAsync("/api/admin/profile", current! with { De = current.De with { Headline = " " } });
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("de.headline", problem!.Errors.Keys);
  }

  [PostgresFact]
  public async Task The_photo_must_be_a_public_image_document()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var current = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");
    var photo = await factory.AddDocumentAsync();
    var linkOnly = await factory.AddDocumentAsync(DocumentKind.Image, DocumentVisibility.LinkOnly);

    var missing = await admin.PutAsJsonAsync("/api/admin/profile", current! with { PhotoDocumentId = 999_999 });
    var hidden = await admin.PutAsJsonAsync("/api/admin/profile", current with { PhotoDocumentId = linkOnly });
    var valid = await admin.PutAsJsonAsync("/api/admin/profile", current with { PhotoDocumentId = photo });
    using var client = factory.CreateClient();
    var shown = await client.GetFromJsonAsync<PublicProfile>("/api/public/profile");

    Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, hidden.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, valid.StatusCode);
    Assert.Equal(photo, shown!.PhotoDocumentId);
  }
}
