using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Shared;

public sealed class LocaleTests
{
  [Theory]
  [InlineData("de")]
  [InlineData("DE")]
  [InlineData(" en ")]
  public void Parses_supported_codes_regardless_of_case_and_whitespace(string code)
  {
    var locale = Locale.Parse(code);

    Assert.Contains(locale, Locale.All);
    Assert.Equal(code.Trim().ToLowerInvariant(), locale.Code);
  }

  [Theory]
  [InlineData("fr")]
  [InlineData("")]
  [InlineData(null)]
  public void Rejects_unsupported_codes_as_validation_errors(string? code)
  {
    var exception = Assert.Throws<DomainValidationException>(() => Locale.Parse(code));

    Assert.Contains("locale", exception.Errors.Keys);
  }

  [Fact]
  public void German_is_the_primary_locale()
  {
    Assert.True(Locale.German.IsPrimary);
    Assert.False(Locale.English.IsPrimary);
    Assert.Equal(Locale.German, Locale.All[0]);
  }

  [Fact]
  public void Try_parse_reports_failure_without_throwing()
  {
    Assert.False(Locale.TryParse("xx", out _));
    Assert.True(Locale.TryParse("en", out var english));
    Assert.Equal(Locale.English, english);
  }
}
