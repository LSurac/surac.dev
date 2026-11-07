using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Tests.Shared;

public sealed class RequireTests
{
  [Fact]
  public void Required_text_is_trimmed()
  {
    Assert.Equal("Wert", Require.Text("  Wert ", "field", 10));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Blank_required_text_is_rejected_under_its_field_name(string? value)
  {
    var exception = Assert.Throws<DomainValidationException>(() => Require.Text(value, "company", 10));

    Assert.Contains("company", exception.Errors.Keys);
  }

  [Fact]
  public void Text_longer_than_its_limit_is_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Require.Text("elf Zeichen", "field", 10));
    Assert.Throws<DomainValidationException>(() => Require.OptionalText("elf Zeichen", "field", 10));
  }

  [Fact]
  public void Blank_optional_text_becomes_null()
  {
    Assert.Null(Require.OptionalText("  ", "field", 10));
    Assert.Equal("x", Require.OptionalText(" x ", "field", 10));
  }

  [Fact]
  public void A_translated_field_is_required_in_german_only_and_keyed_by_locale()
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => Require.Translated(" ", Locale.German, "title", 10, required: true));

    Assert.Contains("de.title", exception.Errors.Keys);
    Assert.Null(Require.Translated(" ", Locale.English, "title", 10, required: true));
  }

  [Theory]
  [InlineData("https://github.com/example")]
  [InlineData("http://example.org/path?q=1")]
  public void Absolute_http_urls_are_accepted(string url)
  {
    Assert.Equal(url, Require.OptionalUrl(url, "url"));
  }

  [Theory]
  [InlineData("github.com/example")]
  [InlineData("javascript:alert(1)")]
  [InlineData("ftp://example.org")]
  public void Anything_but_an_absolute_http_url_is_rejected(string url)
  {
    Assert.Throws<DomainValidationException>(() => Require.OptionalUrl(url, "url"));
  }

  [Fact]
  public void An_email_address_must_look_like_one()
  {
    Assert.Equal("max@example.org", Require.Email(" max@example.org ", "email"));
    Assert.Throws<DomainValidationException>(() => Require.Email("max at example", "email"));
  }

  [Fact]
  public void Lines_are_trimmed_and_blank_lines_dropped()
  {
    var lines = Require.Lines(["  eins ", "", null, "zwei"], "bullets", 10, maxCount: 5);

    Assert.Equal(["eins", "zwei"], lines);
  }

  [Fact]
  public void Too_many_lines_are_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Require.Lines(["a", "b", "c"], "bullets", 10, maxCount: 2));
  }

  [Fact]
  public void An_end_date_before_the_start_date_is_rejected()
  {
    var start = new DateOnly(2024, 5, 1);

    Require.DateRange(start, null);
    Require.DateRange(start, start);
    Assert.Throws<DomainValidationException>(() => Require.DateRange(start, new DateOnly(2024, 4, 30)));
  }

  [Theory]
  [InlineData(1949)]
  [InlineData(2101)]
  public void Years_outside_a_plausible_range_are_rejected(int year)
  {
    Assert.Throws<DomainValidationException>(() => Require.Year(year, "sinceYear"));
  }

  [Fact]
  public void A_missing_year_is_allowed()
  {
    Assert.Null(Require.Year(null, "sinceYear"));
    Assert.Equal(2019, Require.Year(2019, "sinceYear"));
  }

  [Fact]
  public void The_unknown_sentinel_of_an_enum_is_never_accepted()
  {
    Assert.Throws<DomainValidationException>(() => Require.Known(DayOfWeek.Sunday, "day"));
    Assert.Equal(DayOfWeek.Monday, Require.Known(DayOfWeek.Monday, "day"));
    Assert.Throws<DomainValidationException>(() => Require.Known((DayOfWeek)42, "day"));
  }
}
