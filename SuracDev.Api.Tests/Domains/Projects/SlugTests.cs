using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Tests.Domains.Projects;

public sealed class SlugTests
{
  [Theory]
  [InlineData("vereinswebsite")]
  [InlineData("kassen-sturz-2")]
  [InlineData("ab")]
  public void Lowercase_words_joined_by_hyphens_are_slugs(string value)
  {
    Assert.Equal(value, Slug.Parse(value).Value);
  }

  [Fact]
  public void Surrounding_whitespace_and_upper_case_are_normalised()
  {
    Assert.Equal("projekt", Slug.Parse("  Projekt ").Value);
  }

  [Theory]
  [InlineData("")]
  [InlineData("a")]
  [InlineData("-projekt")]
  [InlineData("projekt-")]
  [InlineData("pro--jekt")]
  [InlineData("projekt mit leerzeichen")]
  [InlineData("über")]
  [InlineData("../etc")]
  public void Anything_else_is_rejected(string value)
  {
    var exception = Assert.Throws<DomainValidationException>(() => Slug.Parse(value));

    Assert.Contains("slug", exception.Errors.Keys);
  }

  [Fact]
  public void A_slug_longer_than_eighty_characters_is_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Slug.Parse(new string('a', 81)));
  }

  [Fact]
  public void Try_parse_reports_failure_instead_of_throwing()
  {
    Assert.True(Slug.TryParse("projekt", out var slug));
    Assert.Equal("projekt", slug.Value);
    Assert.False(Slug.TryParse("Kein Slug", out _));
  }
}
