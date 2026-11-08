using SuracDev.Api.Domains.Pdf.Domain;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Pdf;

public sealed class CvFormatTests
{
  [Fact]
  public void A_period_shows_month_and_year_and_names_an_open_end_per_locale()
  {
    var start = new DateOnly(2022, 9, 1);

    Assert.Equal("09/2022 – heute", CvFormat.Period(start, null, Locale.German));
    Assert.Equal("09/2022 – present", CvFormat.Period(start, null, Locale.English));
    Assert.Equal(
      "09/2019 – 06/2022",
      CvFormat.Period(new DateOnly(2019, 9, 1), new DateOnly(2022, 6, 30), Locale.German));
  }

  [Fact]
  public void A_skill_names_its_since_year_and_qualifier_never_a_level()
  {
    Assert.Equal("C# (seit 2019)", CvFormat.Skill("C#", 2019, null, Locale.German));
    Assert.Equal("VB.NET (Legacy, since 2019)", CvFormat.Skill("VB.NET", 2019, "Legacy", Locale.English));
    Assert.Equal("Docker", CvFormat.Skill("Docker", null, null, Locale.German));
  }

  [Theory]
  [InlineData("Max Mustermann", "de", "Lebenslauf_Max_Mustermann.pdf")]
  [InlineData("Max Mustermann", "en", "CV_Max_Mustermann.pdf")]
  [InlineData("  Jörg  Müller-Lüdenscheid ", "de", "Lebenslauf_Joerg_Mueller-Luedenscheid.pdf")]
  public void The_file_name_is_ascii_with_the_name_and_a_localized_prefix(string name, string locale, string expected)
  {
    Assert.Equal(expected, CvFormat.FileName(name, Locale.Parse(locale)));
  }
}
