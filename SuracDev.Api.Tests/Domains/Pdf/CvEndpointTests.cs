using System.Net;
using System.Net.Http.Json;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Tests.Shared;
using UglyToad.PdfPig;

namespace SuracDev.Api.Tests.Domains.Pdf;

[Collection(PostgresCollection.Name)]
public sealed class CvEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task The_public_cv_is_a_german_pdf_by_default_with_a_named_file()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/public/cv.pdf");
    var text = PdfText(await response.Content.ReadAsByteArrayAsync());

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
    Assert.Equal("Lebenslauf_Max_Mustermann.pdf", response.Content.Headers.ContentDisposition!.FileNameStar);
    Assert.Contains("Max Mustermann", text, StringComparison.Ordinal);
    Assert.Contains("Berufserfahrung", text, StringComparison.Ordinal);
    Assert.Contains("Beispiel GmbH", text, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task The_english_cv_has_english_headings_and_its_own_file_name()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/public/cv.pdf?locale=en");
    var text = PdfText(await response.Content.ReadAsByteArrayAsync());

    Assert.Equal("CV_Max_Mustermann.pdf", response.Content.Headers.ContentDisposition!.FileNameStar);
    Assert.Contains("Experience", text, StringComparison.Ordinal);
    Assert.Contains("Full-stack developer", text, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task The_public_cv_never_contains_phone_or_street_address_nor_hidden_entries()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var admin = await factory.CreateLoggedInClientAsync())
    {
      var profile = await admin.GetFromJsonAsync<AdminProfile>("/api/admin/profile");
      await admin.PutAsJsonAsync("/api/admin/profile", profile! with { Phone = "+49 000 111222" });
      await admin.PostAsJsonAsync(
        "/api/admin/experiences",
        new ExperienceForm(
          "Versteckte AG", null, new DateOnly(2018, 1, 1), new DateOnly(2018, 12, 31), 0, IsVisible: false,
          new ExperienceTextsForm("Geheim", null, [], null), null));
    }

    using var client = factory.CreateClient();
    var text = PdfText(await client.GetByteArrayAsync("/api/public/cv.pdf"));

    Assert.DoesNotContain("000 111222", text, StringComparison.Ordinal);
    Assert.DoesNotContain("Musterstraße", text, StringComparison.Ordinal);
    Assert.DoesNotContain("Versteckte AG", text, StringComparison.Ordinal);
  }

  [PostgresFact]
  public async Task An_unsupported_locale_is_a_validation_error()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/api/public/cv.pdf?locale=fr");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  private static string PdfText(byte[] pdf)
  {
    using var document = PdfDocument.Open(pdf);
    return string.Concat(document.GetPages().Select(GoldenFile.PageText));
  }
}
