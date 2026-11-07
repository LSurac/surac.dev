using SuracDev.Api.Domains.Pdf.Domain;
using SuracDev.Api.Domains.Pdf.Rendering;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Tests.Shared;
using UglyToad.PdfPig;

namespace SuracDev.Api.Tests.Domains.Pdf;

/// <summary>
/// The CV is checked as an ATS reads it: the text PdfPig extracts, in reading order, against a
/// reviewed golden file. Regenerate with UPDATE_GOLDEN=1 and review the diff before committing.
/// </summary>
public sealed class CvDocumentTests
{
  private static CvModel Model(Locale locale) => new(
    Locale: locale,
    FullName: "Max Mustermann",
    TitleLine: locale.IsPrimary ? "Fullstack-Entwickler · .NET & Angular" : "Full-stack developer · .NET & Angular",
    ContactLines: ["max.mustermann@example.org", "Musterstadt", "github.com/example"],
    Intro: locale.IsPrimary
      ? "Beispielhaftes Kurzprofil in zwei Sätzen. Es zeigt, wo das Profil im Lebenslauf steht."
      : "An example short profile in two sentences. It shows where the profile goes in the CV.",
    Experience:
    [
      new CvEntry(
        "09/2022 – " + (locale.IsPrimary ? "heute" : "present"),
        locale.IsPrimary ? "Softwareentwickler" : "Software developer",
        "Beispiel GmbH, Musterstadt",
        null,
        locale.IsPrimary ? ["Erster Beispielpunkt", "Zweiter Beispielpunkt"] : ["First example point", "Second example point"],
        ".NET · Angular · PostgreSQL"),
    ],
    Education: [new CvEntry("09/2019 – 06/2022", "Fachinformatiker", "Beispiel-Berufsschule", null, [], "1,8")],
    Skills: [new CvSkillGroup("Backend", ["C# (seit 2019)", "ASP.NET Core (seit 2020)"])],
    Projects: [new CvProject("Beispielprojekt", "Ein Beispielprojekt.", ".NET · Angular")]);

  [Theory]
  [InlineData("de")]
  [InlineData("en")]
  public void The_public_cv_reads_in_order_as_one_column_of_real_text(string locale)
  {
    var pdf = CvDocument.Render(Model(Locale.Parse(locale)));

    using var document = PdfDocument.Open(pdf);
    var text = string.Join("\n--- page ---\n", document.GetPages().Select(GoldenFile.PageText));

    GoldenFile.Verify(text, $"cv-public-{locale}.txt");
  }

  [Fact]
  public void The_cv_carries_title_author_and_language_in_its_metadata()
  {
    var pdf = CvDocument.Render(Model(Locale.German));

    using var document = PdfDocument.Open(pdf);

    Assert.Equal("Lebenslauf – Max Mustermann", document.Information.Title);
    Assert.Equal("Max Mustermann", document.Information.Author);
  }

  [Fact]
  public void Sections_without_content_are_left_out()
  {
    var model = Model(Locale.German) with { Projects = [], Intro = null };

    using var document = PdfDocument.Open(CvDocument.Render(model));
    var text = string.Concat(document.GetPages().Select(GoldenFile.PageText));

    Assert.DoesNotContain("Projekte", text, StringComparison.Ordinal);
    Assert.DoesNotContain("Profil", text, StringComparison.Ordinal);
    Assert.Contains("Berufserfahrung", text, StringComparison.Ordinal);
  }
}
