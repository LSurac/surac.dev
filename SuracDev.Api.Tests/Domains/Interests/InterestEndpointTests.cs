using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Interests.Contracts;
using SuracDev.Api.Domains.Interests.Domain;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Interests;

[Collection(PostgresCollection.Name)]
public sealed class InterestEndpointTests(PostgresFixture postgres)
{
  private static readonly InterestForm NewInterest = new(
    Kind: InterestKind.Chess,
    SortOrder: -1,
    IsVisible: true,
    ImageDocumentId: null,
    De: new InterestTextsForm("Schach", "Spiele im **Verein**."),
    En: new InterestTextsForm("Chess", null));

  [PostgresFact]
  public async Task A_fresh_installation_shows_one_example_interest()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var interests = await client.GetFromJsonAsync<List<PublicInterest>>("/api/public/interests", ApiJson.Options);

    var example = Assert.Single(interests!);
    Assert.Contains("Beispiel", example.Title, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task The_public_page_lists_visible_interests_in_order_with_rendered_markdown()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    var photo = await factory.AddDocumentAsync(DocumentKind.Image);
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/interests", NewInterest with { ImageDocumentId = photo }, ApiJson.Options);
      await admin.PostAsJsonAsync(
        "/api/admin/interests", NewInterest with { Kind = InterestKind.Music, IsVisible = false }, ApiJson.Options);
    }

    using var client = factory.CreateClient();
    var interests = await client.GetFromJsonAsync<List<PublicInterest>>("/api/public/interests", ApiJson.Options);

    Assert.Equal(2, interests!.Count);
    var chess = interests[0];
    Assert.Equal(InterestKind.Chess, chess.Kind);
    Assert.Equal("Schach", chess.Title);
    Assert.Equal("<p>Spiele im <strong>Verein</strong>.</p>", chess.BodyHtml);
    Assert.Equal(photo, chess.ImageDocumentId);
    Assert.Equal("Schach", chess.ImageAlt);
  }

  [PostgresFact]
  public async Task English_interests_fall_back_to_german_per_field()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      await admin.PostAsJsonAsync("/api/admin/interests", NewInterest, ApiJson.Options);
    }

    using var client = factory.CreateClient();
    var interests = await client.GetFromJsonAsync<List<PublicInterest>>("/api/public/interests?locale=en", ApiJson.Options);
    var chess = interests!.Single(interest => interest.Kind == InterestKind.Chess);

    Assert.Equal("Chess", chess.Title);
    Assert.Equal("<p>Spiele im <strong>Verein</strong>.</p>", chess.BodyHtml);
    Assert.Equal(["bodyHtml"], chess.FallbackFields);
  }

  [PostgresFact]
  public async Task The_admin_creates_reads_updates_and_deletes_an_interest()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/interests", NewInterest, ApiJson.Options);
    var location = created.Headers.Location!;
    var updated = await admin.PutAsJsonAsync(location, NewInterest with { En = null, IsVisible = false }, ApiJson.Options);
    var read = await admin.GetFromJsonAsync<AdminInterest>(location, ApiJson.Options);
    var listed = await admin.GetFromJsonAsync<List<AdminInterest>>("/api/admin/interests", ApiJson.Options);
    var deleted = await admin.DeleteAsync(location);
    var gone = await admin.GetAsync(location);

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal("Spiele im **Verein**.", read!.De.Body);
    Assert.Null(read.En);
    Assert.False(read.IsVisible);
    Assert.Equal(2, listed!.Count);
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
  }

  [PostgresFact]
  public async Task An_interest_needs_a_german_title_and_a_public_image()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    var certificate = await factory.AddDocumentAsync(DocumentKind.Certificate, DocumentVisibility.LinkOnly, DocumentContentTypes.Pdf);
    using var admin = await factory.CreateLoggedInClientAsync();

    var untitled = await admin.PostAsJsonAsync(
      "/api/admin/interests", NewInterest with { De = new InterestTextsForm(" ", null) }, ApiJson.Options);
    var leaking = await admin.PostAsJsonAsync(
      "/api/admin/interests", NewInterest with { ImageDocumentId = certificate }, ApiJson.Options);

    Assert.Equal(HttpStatusCode.BadRequest, untitled.StatusCode);
    Assert.Contains("de.title", (await untitled.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    Assert.Equal(HttpStatusCode.BadRequest, leaking.StatusCode);
    Assert.Contains("imageDocumentId", (await leaking.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
  }
}
