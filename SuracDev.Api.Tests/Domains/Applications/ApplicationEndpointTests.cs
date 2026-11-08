using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Applications;

[Collection(PostgresCollection.Name)]
public sealed class ApplicationEndpointTests(PostgresFixture postgres)
{
  public static readonly ApplicationForm NewApplication = new(
    Company: "Beispiel AG",
    Position: "Fullstack-Entwickler",
    JobAdUrl: "https://example.org/jobs/1",
    AppliedOn: new DateOnly(2026, 11, 1),
    Status: ApplicationStatus.Sent,
    Notes: "Kontakt über **Recruiterin**");

  public static readonly ApplicationLinkForm NewLink = new(
    AudienceProfile.TechLead,
    GreetingDe: "Für das Team Backend",
    GreetingEn: null,
    ExpiresAt: null,
    UnlockFullPdf: true,
    DocumentIds: []);

  [PostgresFact]
  public async Task The_admin_creates_reads_updates_and_deletes_an_application()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var created = await admin.PostAsJsonAsync("/api/admin/applications", NewApplication, ApiJson.Options);
    var location = created.Headers.Location!;
    var updated = await admin.PutAsJsonAsync(
      location,
      NewApplication with { Status = ApplicationStatus.Interview },
      ApiJson.Options);
    var read = await admin.GetFromJsonAsync<AdminApplication>(location, ApiJson.Options);
    var deleted = await admin.DeleteAsync(location);
    var gone = await admin.GetAsync(location);

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal("Beispiel AG", read!.Company);
    Assert.Equal(ApplicationStatus.Interview, read.Status);
    Assert.Equal("Kontakt über **Recruiterin**", read.Notes);
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
  }

  [PostgresFact]
  public async Task Applications_are_listed_newest_first()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    await admin.PostAsJsonAsync(
      "/api/admin/applications",
      NewApplication with { Company = "Erste GmbH" },
      ApiJson.Options);
    await admin.PostAsJsonAsync(
      "/api/admin/applications",
      NewApplication with { Company = "Zweite GmbH" },
      ApiJson.Options);
    var applications = await admin.GetFromJsonAsync<List<AdminApplication>>("/api/admin/applications", ApiJson.Options);

    Assert.Equal(["Zweite GmbH", "Erste GmbH"], applications!.Select(application => application.Company));
  }

  [PostgresFact]
  public async Task An_application_without_company_or_status_is_a_validation_error()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();

    var response = await admin.PostAsJsonAsync(
      "/api/admin/applications",
      NewApplication with { Company = " ", Status = ApplicationStatus.Unknown },
      ApiJson.Options);
    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Contains("company", problem!.Errors.Keys);
  }

  [PostgresFact]
  public async Task A_new_link_shows_its_token_once_and_stores_only_the_hash()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);

    var response = await admin.PostAsJsonAsync(
      $"/api/admin/applications/{applicationId}/links",
      NewLink,
      ApiJson.Options);
    var created = await response.Content.ReadFromJsonAsync<CreatedApplicationLink>(ApiJson.Options);
    var listed = await admin.GetStringAsync($"/api/admin/applications/{applicationId}/links");
    var storedHash = await factory.WithDbContextAsync(
      db => db.ApplicationLinks.Select(link => link.TokenHash).SingleAsync());

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(22, created!.Token.Length);
    Assert.Equal(ApplicationLinkToken.Hash(created.Token), storedHash);
    Assert.DoesNotContain(created.Token, listed, StringComparison.Ordinal);
    Assert.DoesNotContain(storedHash, listed, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task A_link_without_an_expiry_expires_after_ninety_days()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);

    var link = await CreateLinkAsync(admin, applicationId, NewLink);

    Assert.InRange(
      link.Link.ExpiresAt - link.Link.CreatedAt,
      TimeSpan.FromDays(90),
      TimeSpan.FromDays(90).Add(TimeSpan.FromSeconds(5)));
    Assert.True(link.Link.IsActive);
  }

  [PostgresFact]
  public async Task A_link_unlocks_link_only_documents_and_rejects_public_ones()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);
    var certificate = await factory.AddDocumentAsync(
      DocumentKind.Certificate,
      DocumentVisibility.LinkOnly,
      DocumentContentTypes.Pdf);
    var photo = await factory.AddDocumentAsync();

    var rejected = await admin.PostAsJsonAsync(
      $"/api/admin/applications/{applicationId}/links",
      NewLink with { DocumentIds = [certificate, photo] },
      ApiJson.Options);
    var unknown = await admin.PostAsJsonAsync(
      $"/api/admin/applications/{applicationId}/links", NewLink with { DocumentIds = [987654] }, ApiJson.Options);
    var link = await CreateLinkAsync(admin, applicationId, NewLink with { DocumentIds = [certificate] });

    Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    Assert.Equal([certificate], link.Link.DocumentIds);
  }

  [PostgresFact]
  public async Task Updating_a_link_replaces_its_settings_and_documents()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);
    var certificate = await factory.AddDocumentAsync(
      DocumentKind.Certificate,
      DocumentVisibility.LinkOnly,
      DocumentContentTypes.Pdf);
    var coverLetter = await factory.AddDocumentAsync(
      DocumentKind.CoverLetter,
      DocumentVisibility.LinkOnly,
      DocumentContentTypes.Pdf);
    var link = await CreateLinkAsync(admin, applicationId, NewLink with { DocumentIds = [certificate] });
    var path = $"/api/admin/applications/{applicationId}/links/{link.Link.Id}";

    var updated = await admin.PutAsJsonAsync(
      path,
      NewLink with
      {
        Profile = AudienceProfile.Recruiter,
        UnlockFullPdf = false,
        DocumentIds = [certificate, coverLetter],
      },
      ApiJson.Options);
    var read = await admin.GetFromJsonAsync<AdminApplicationLink>(path, ApiJson.Options);

    Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    Assert.Equal(AudienceProfile.Recruiter, read!.Profile);
    Assert.False(read.UnlockFullPdf);
    Assert.Equal([certificate, coverLetter], read.DocumentIds.Order());
  }

  [PostgresFact]
  public async Task A_revoked_link_is_inactive_and_can_no_longer_be_changed()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);
    var link = await CreateLinkAsync(admin, applicationId, NewLink);
    var path = $"/api/admin/applications/{applicationId}/links/{link.Link.Id}";

    var revoked = await admin.PostAsync($"{path}/revoke", null);
    var changed = await admin.PutAsJsonAsync(path, NewLink, ApiJson.Options);
    var read = await admin.GetFromJsonAsync<AdminApplicationLink>(path, ApiJson.Options);

    Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    Assert.True(read!.IsRevoked);
    Assert.False(read.IsActive);
  }

  [PostgresFact]
  public async Task A_link_is_only_found_under_its_own_application()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var first = await CreateApplicationAsync(admin);
    var second = await CreateApplicationAsync(admin);
    var link = await CreateLinkAsync(admin, first, NewLink);

    var response = await admin.GetAsync($"/api/admin/applications/{second}/links/{link.Link.Id}");
    var missingApplication = await admin.PostAsJsonAsync(
      "/api/admin/applications/987654/links",
      NewLink,
      ApiJson.Options);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, missingApplication.StatusCode);
  }

  [PostgresFact]
  public async Task Deleting_an_application_removes_its_links()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var admin = await factory.CreateLoggedInClientAsync();
    var applicationId = await CreateApplicationAsync(admin);
    var certificate = await factory.AddDocumentAsync(
      DocumentKind.Certificate,
      DocumentVisibility.LinkOnly,
      DocumentContentTypes.Pdf);
    await CreateLinkAsync(admin, applicationId, NewLink with { DocumentIds = [certificate] });

    await admin.DeleteAsync($"/api/admin/applications/{applicationId}");
    var links = await factory.WithDbContextAsync(db => db.ApplicationLinks.CountAsync());
    var attachments = await factory.WithDbContextAsync(
      db => db.ApplicationLinks.SelectMany(link => link.Documents).CountAsync());

    Assert.Equal(0, links);
    Assert.Equal(0, attachments);
  }

  public static async Task<long> CreateApplicationAsync(HttpClient admin)
  {
    var response = await admin.PostAsJsonAsync("/api/admin/applications", NewApplication, ApiJson.Options);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AdminApplication>(ApiJson.Options))!.Id;
  }

  public static async Task<CreatedApplicationLink> CreateLinkAsync(
    HttpClient admin,
    long applicationId,
    ApplicationLinkForm form)
  {
    var response = await admin.PostAsJsonAsync($"/api/admin/applications/{applicationId}/links", form, ApiJson.Options);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<CreatedApplicationLink>(ApiJson.Options))!;
  }
}
