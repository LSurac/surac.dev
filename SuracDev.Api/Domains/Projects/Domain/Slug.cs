using System.Text.RegularExpressions;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Projects.Domain;

/// <summary>
/// The URL segment of a case study (<c>/projekte/&lt;slug&gt;</c>, shared by both locales): lowercase
/// ASCII words joined by single hyphens. A value object, so no route ever sees an unchecked string.
/// </summary>
public readonly partial record struct Slug
{
  public const int MinLength = 2;
  public const int MaxLength = 80;

  private Slug(string value) => Value = value;

  public string Value { get; }

  public static Slug Parse(string? value)
  {
    var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
    return normalized.Length is >= MinLength and <= MaxLength && Pattern().IsMatch(normalized)
      ? new Slug(normalized)
      : throw new DomainValidationException(
        "slug",
        "Lowercase letters and digits joined by single hyphens, 2 to 80 characters.");
  }

  public override string ToString() => Value;

  [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
  private static partial Regex Pattern();
}
