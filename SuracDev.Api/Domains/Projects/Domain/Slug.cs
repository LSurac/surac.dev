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

  /// <summary>Parses admin input; trims and lower-cases it first.</summary>
  /// <exception cref="DomainValidationException">The value is not a valid slug.</exception>
  public static Slug Parse(string? value)
    => TryParse(value, out var slug)
      ? slug
      : throw new DomainValidationException(
        "slug",
        "Lowercase letters and digits joined by single hyphens, 2 to 80 characters.");

  /// <summary>Parses a slug from a URL, where an invalid value simply names no project.</summary>
  public static bool TryParse(string? value, out Slug slug)
  {
    var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
    var isValid = normalized.Length is >= MinLength and <= MaxLength && Pattern().IsMatch(normalized);
    slug = isValid ? new Slug(normalized) : default;
    return isValid;
  }

  public override string ToString() => Value;

  [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
  private static partial Regex Pattern();
}
