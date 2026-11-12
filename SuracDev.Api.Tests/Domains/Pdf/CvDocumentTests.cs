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
        locale.IsPrimary
          ? ["Erster Beispielpunkt", "Zweiter Beispielpunkt"]
          : ["First example point", "Second example point"],
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
  public void A_cv_of_five_working_years_fits_on_two_pages()
  {
    using var document = PdfDocument.Open(CvDocument.Render(FiveYearModel()));

    Assert.Equal(2, document.NumberOfPages);
  }

  /// <summary>Neutral text in the volume of the maintainer's real CV in November 2026 (five stations).</summary>
  private static CvModel FiveYearModel() => Model(Locale.German) with
  {
    Intro = Filler(520),
    Experience =
    [
      Station(summary: 230, bullets: [120, 110, 100, 110, 120, 90]),
      Station(summary: 230, bullets: [110, 80, 80, 100, 80]),
      Station(summary: 170, bullets: [100, 90, 90, 100, 60]),
      Station(summary: 0, bullets: [80, 80]),
      Station(summary: 70, bullets: []),
    ],
    Education =
    [
      new CvEntry("10/2021 – 06/2024", "Fachinformatiker", "Beispiel-Berufsschule, Musterstadt", null, [], "1,8"),
      new CvEntry("10/2017 – 02/2020", "Studium", "Beispiel-Hochschule, Musterstadt", null, [], null),
      new CvEntry("09/2008 – 07/2016", "Hochschulreife", "Beispiel-Gymnasium, Musterstadt", null, [], "2,0"),
    ],
    Skills = [.. new[] { 170, 200, 110, 100, 150, 190, 80 }.Select((length, index) =>
      new CvSkillGroup($"Gruppe {index + 1}", [Filler(length)]))],
    Projects = [.. new[] { 190, 160, 150, 160 }.Select(length =>
      new CvProject("Beispielprojekt", Filler(length), ".NET · Angular · PostgreSQL · Docker"))],
  };

  private static CvEntry Station(int summary, int[] bullets) => new(
    "05/2026 – heute",
    "Softwareentwickler",
    "Beispiel GmbH, Musterstadt",
    summary > 0 ? Filler(summary) : null,
    [.. bullets.Select(Filler)],
    "C# · .NET · ASP.NET Core · Angular · SQL Server · Docker");

  /// <summary>Words of typical German length, about <paramref name="length"/> characters.</summary>
  private static string Filler(int length)
  {
    var words = new List<string>();
    var index = 0;
    string[] pool = ["Beispieltext", "für", "eine", "Beschreibung", "mit", "typischer", "Länge", "und", "Inhalt"];
    while (words.Sum(word => word.Length + 1) < length)
    {
      words.Add(pool[index++ % pool.Length]);
    }

    return string.Join(' ', words);
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
