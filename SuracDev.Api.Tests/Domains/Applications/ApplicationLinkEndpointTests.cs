using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Documents.Contracts;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Tests.Shared;
using UglyToad.PdfPig;
using static SuracDev.Api.Tests.Domains.Applications.ApplicationEndpointTests;

namespace SuracDev.Api.Tests.Domains.Applications;

[Collection(PostgresCollection.Name)]
public sealed class ApplicationLinkEndpointTests(PostgresFixture postgres)
{
  private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF\n");

  [PostgresFact]
  public async Task An_active_link_shows_its_greeting_section_order_and_documents()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var certificate = await UploadAsync(admin, "zeugnis.pdf", DocumentKind.Certificate);
    var link = await CreateLinkAsync(
      admin,
      await CreateApplicationAsync(admin),
      NewLink with { DocumentIds = [certificate] });

    using var client = factory.CreateClient();
    var german = await client.GetFromJsonAsync<PublicApplicationLink>($"/api/links/{link.Token}", ApiJson.Options);
    var english = await client.GetFromJsonAsync<PublicApplicationLink>(
      $"/api/links/{link.Token}?locale=en",
      ApiJson.Options);

    Assert.Equal("Für das Team Backend", german!.Greeting);
    Assert.Equal("Für das Team Backend", english!.Greeting);
    Assert.Equal(SectionOrder.For(AudienceProfile.TechLead), german.Sections);
    Assert.True(german.FullCv);
    var document = Assert.Single(german.Documents);
    Assert.Equal(certificate, document.Id);
    Assert.Equal("Zeugnis", document.Label);
    Assert.Equal("zeugnis.pdf", document.FileName);
  }

  [PostgresFact]
  public async Task Opening_a_link_records_the_event_with_its_locale_and_nothing_about_the_visitor()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var link = await CreateLinkAsync(admin, await CreateApplicationAsync(admin), NewLink);

    using var client = factory.CreateClient();
    await client.GetAsync($"/api/links/{link.Token}?locale=en");
    var events = await factory.WithDbContextAsync(db => db.LinkEvents.ToListAsync());

    var opened = Assert.Single(events);
    Assert.Equal(LinkEventKind.Opened, opened.Kind);
    Assert.Equal("en", opened.Locale.Code);
    Assert.Equal(link.Link.Id, opened.LinkId);
    Assert.Null(opened.DocumentId);
  }

  [PostgresFact]
  public async Task Unknown_expired_and_revoked_links_answer_not_found_alike()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);
    var expired = await CreateLinkAsync(admin, applicationId, NewLink);
    var revoked = await CreateLinkAsync(admin, applicationId, NewLink);
    await factory.WithDbContextAsync(db => db.ApplicationLinks.Where(link => link.Id == expired.Link.Id)
      .ExecuteUpdateAsync(set => set.SetProperty(link => link.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));
    await admin.PostAsync($"/api/admin/applications/{applicationId}/links/{revoked.Link.Id}/revoke", null);

    using var client = factory.CreateClient();
    var unknown = await client.GetAsync($"/api/links/{ApplicationLinkToken.Generate().Token}");
    var expiredResponse = await client.GetAsync($"/api/links/{expired.Token}");
    var revokedResponse = await client.GetAsync($"/api/links/{revoked.Token}");
    var revokedCv = await client.GetAsync($"/api/links/{revoked.Token}/cv.pdf");
    var events = await factory.WithDbContextAsync(db => db.LinkEvents.CountAsync());

    Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, expiredResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, revokedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, revokedCv.StatusCode);
    Assert.Equal(
      (await unknown.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail,
      (await revokedResponse.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    Assert.Equal(0, events);
  }

  [PostgresFact]
  public async Task The_link_cv_is_the_full_variant_only_when_the_link_unlocks_it()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var profile = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");
    await admin.PutAsJsonAsync("/api/admin/profile", profile! with { Phone = "+49 000 111222" });
    var applicationId = await CreateApplicationAsync(admin);
    var full = await CreateLinkAsync(admin, applicationId, NewLink with { UnlockFullPdf = true });
    var reduced = await CreateLinkAsync(admin, applicationId, NewLink with { UnlockFullPdf = false });

    using var client = factory.CreateClient();
    var fullResponse = await client.GetAsync($"/api/links/{full.Token}/cv.pdf?locale=en");
    var fullText = PdfText(await fullResponse.Content.ReadAsByteArrayAsync());
    var reducedText = PdfText(await client.GetByteArrayAsync($"/api/links/{reduced.Token}/cv.pdf"));
    var events = await factory.WithDbContextAsync(db => db.LinkEvents.OrderBy(e => e.Id).ToListAsync());

    Assert.Equal("application/pdf", fullResponse.Content.Headers.ContentType!.MediaType);
    Assert.Equal("CV_Max_Mustermann.pdf", fullResponse.Content.Headers.ContentDisposition!.FileNameStar);
    Assert.Contains("+49 000 111222", fullText, StringComparison.Ordinal);
    Assert.DoesNotContain("000 111222", reducedText, StringComparison.Ordinal);
    Assert.All(events, e => Assert.Equal(LinkEventKind.PdfDownloaded, e.Kind));
    Assert.Equal([full.Link.Id, reduced.Link.Id], events.Select(e => e.LinkId));
  }

  [PostgresFact]
  public async Task A_link_delivers_its_own_documents_and_no_others()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var attached = await UploadAsync(admin, "zeugnis.pdf", DocumentKind.Certificate);
    var other = await UploadAsync(admin, "anschreiben.pdf", DocumentKind.CoverLetter);
    var link = await CreateLinkAsync(
      admin,
      await CreateApplicationAsync(admin),
      NewLink with { DocumentIds = [attached] });

    using var client = factory.CreateClient();
    var delivered = await client.GetAsync($"/api/links/{link.Token}/documents/{attached}");
    var refused = await client.GetAsync($"/api/links/{link.Token}/documents/{other}");
    var publicRoute = await client.GetAsync($"/api/public/documents/{attached}");
    var recorded = await factory.WithDbContextAsync(db => db.LinkEvents.SingleAsync());

    Assert.Equal(HttpStatusCode.OK, delivered.StatusCode);
    Assert.Equal(Pdf, await delivered.Content.ReadAsByteArrayAsync());
    Assert.Equal("attachment", delivered.Content.Headers.ContentDisposition!.DispositionType);
    Assert.Equal("zeugnis.pdf", delivered.Content.Headers.ContentDisposition.FileNameStar);
    Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, publicRoute.StatusCode);
    Assert.Equal(LinkEventKind.DocumentDownloaded, recorded.Kind);
    Assert.Equal(attached, recorded.DocumentId);
  }

  [PostgresFact]
  public async Task Link_resolution_is_rate_limited_per_client()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();
    var token = ApplicationLinkToken.Generate().Token;

    var statuses = new List<HttpStatusCode>();
    for (var attempt = 0; attempt < 31; attempt++)
    {
      statuses.Add((await client.GetAsync($"/api/links/{token}")).StatusCode);
    }

    Assert.All(statuses.Take(30), status => Assert.Equal(HttpStatusCode.NotFound, status));
    Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
  }

  private static async Task<long> UploadAsync(HttpClient admin, string fileName, DocumentKind kind)
  {
    var file = new ByteArrayContent(Pdf);
    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    var form = new MultipartFormDataContent
    {
      { file, "file", fileName },
      { new StringContent(kind.ToString()), "kind" },
      { new StringContent(nameof(DocumentVisibility.LinkOnly)), "visibility" },
      { new StringContent("Zeugnis"), "labelDe" },
    };
    var response = await admin.PostAsync("/api/admin/documents", form);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AdminDocument>(ApiJson.Options))!.Id;
  }

  private static string PdfText(byte[] pdf)
  {
    using var document = PdfDocument.Open(pdf);
    return string.Concat(document.GetPages().Select(GoldenFile.PageText));
  }
}
