using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Documents.Contracts;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Documents;

[Collection(PostgresCollection.Name)]
public sealed class DocumentEndpointTests(PostgresFixture postgres)
{
  private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
  private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF\n");

  [PostgresFact]
  public async Task An_uploaded_public_image_is_served_anonymously_with_an_etag()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var uploaded = await UploadAsync(admin, Png, "portrait.png", DocumentKind.Photo, DocumentVisibility.Public);
    var document = (await uploaded.Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!;
    using var client = factory.CreateClient();
    var served = await client.GetAsync($"/api/public/documents/{document.Id}");
    var bytes = await served.Content.ReadAsByteArrayAsync();
    using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/public/documents/{document.Id}");
    conditional.Headers.IfNoneMatch.Add(served.Headers.ETag!);
    var notModified = await client.SendAsync(conditional);

    Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
    Assert.Equal("image/png", document.ContentType);
    Assert.Equal(Png.Length, document.SizeBytes);
    Assert.Equal(Png, bytes);
    Assert.Equal("image/png", served.Content.Headers.ContentType!.MediaType);
    Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
  }

  [PostgresFact]
  public async Task The_content_type_comes_from_the_bytes_not_from_the_name()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var disguised = await UploadAsync(
      admin, Encoding.ASCII.GetBytes("<svg onload=alert(1)>"), "bild.png", DocumentKind.Image, DocumentVisibility.Public);
    var problem = await disguised.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, disguised.StatusCode);
    Assert.Contains("file", problem!.Errors.Keys);
    Assert.Empty(Directory.EnumerateFiles(factory.DocumentsPath, "*", SearchOption.AllDirectories));
  }

  [PostgresFact]
  public async Task A_rejected_document_leaves_no_file_behind()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await UploadAsync(admin, Pdf, "zeugnis.pdf", DocumentKind.Certificate, DocumentVisibility.Public);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Empty(Directory.EnumerateFiles(factory.DocumentsPath, "*", SearchOption.AllDirectories));
  }

  [PostgresFact]
  public async Task Link_only_documents_are_never_served_publicly_but_the_admin_can_download_them()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var uploaded = await UploadAsync(admin, Pdf, "zeugnis.pdf", DocumentKind.Certificate, DocumentVisibility.LinkOnly);
    var document = (await uploaded.Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!;

    using var client = factory.CreateClient();
    var anonymous = await client.GetAsync($"/api/public/documents/{document.Id}");
    var download = await admin.GetAsync($"/api/admin/documents/{document.Id}/content");

    Assert.Equal(HttpStatusCode.NotFound, anonymous.StatusCode);
    Assert.Equal(HttpStatusCode.OK, download.StatusCode);
    Assert.Equal("zeugnis.pdf", download.Content.Headers.ContentDisposition!.FileNameStar);
    Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync());
  }

  [PostgresFact]
  public async Task The_admin_lists_relabels_and_changes_the_visibility_of_documents()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var uploaded = await UploadAsync(admin, Png, "bild.png", DocumentKind.Image, DocumentVisibility.Public);
    var document = (await uploaded.Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!;

    var updated = await admin.PutAsJsonAsync(
      $"/api/admin/documents/{document.Id}",
      new DocumentUpdateForm(DocumentVisibility.LinkOnly, "Neues Label", "New label"),
      ApiJson.Options);
    var list = (await admin.GetFromJsonAsync<List<AdminDocument>>("/api/admin/documents", ApiJson.Options))!;
    using var client = factory.CreateClient();
    var hidden = await client.GetAsync($"/api/public/documents/{document.Id}");

    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    var listed = Assert.Single(list);
    Assert.Equal(DocumentVisibility.LinkOnly, listed.Visibility);
    Assert.Equal("New label", listed.LabelEn);
    Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
  }

  [PostgresFact]
  public async Task A_document_in_use_cannot_be_deleted_or_hidden_an_unused_one_disappears_with_its_file()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var photo = (await (await UploadAsync(admin, Png, "foto.png", DocumentKind.Photo, DocumentVisibility.Public))
      .Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!;
    var spare = (await (await UploadAsync(admin, Png, "frei.png", DocumentKind.Image, DocumentVisibility.Public))
      .Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!;
    var profile = (await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile"))!;
    await admin.PutAsJsonAsync("/api/admin/profile", profile with { PhotoDocumentId = photo.Id });

    var blocked = await admin.DeleteAsync($"/api/admin/documents/{photo.Id}");
    var hide = await admin.PutAsJsonAsync(
      $"/api/admin/documents/{photo.Id}",
      new DocumentUpdateForm(DocumentVisibility.LinkOnly, null, null),
      ApiJson.Options);
    var deleted = await admin.DeleteAsync($"/api/admin/documents/{spare.Id}");
    var gone = await admin.GetAsync($"/api/admin/documents/{spare.Id}/content");

    Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, hide.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    Assert.Single(Directory.EnumerateFiles(factory.DocumentsPath, "*", SearchOption.AllDirectories));
  }

  [PostgresFact]
  public async Task Files_over_ten_megabytes_are_refused()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var large = new byte[(10 * 1024 * 1024) + 1];
    Png.CopyTo(large, 0);

    var response = await UploadAsync(admin, large, "gross.png", DocumentKind.Image, DocumentVisibility.Public);

    Assert.True(
      response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge,
      $"Expected 400 or 413, got {(int)response.StatusCode}.");
    Assert.Empty(Directory.EnumerateFiles(factory.DocumentsPath, "*", SearchOption.AllDirectories));
  }

  [PostgresFact]
  public async Task Unknown_documents_answer_404()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    using var client = factory.CreateClient();

    Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/documents/999999")).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/admin/documents/999999")).StatusCode);
  }

  private static Task<HttpResponseMessage> UploadAsync(
    HttpClient client,
    byte[] content,
    string fileName,
    DocumentKind kind,
    DocumentVisibility visibility)
  {
    var file = new ByteArrayContent(content);
    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    var form = new MultipartFormDataContent
    {
      { file, "file", fileName },
      { new StringContent(kind.ToString()), "kind" },
      { new StringContent(visibility.ToString()), "visibility" },
      { new StringContent("Label"), "labelDe" },
    };
    return client.PostAsync("/api/admin/documents", form);
  }
}
