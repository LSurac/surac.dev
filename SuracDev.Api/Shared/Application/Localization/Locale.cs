using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Shared.Application.Localization;

/// <summary>
/// One of the two supported content languages. German is the primary locale every translation falls
/// back to. Stored as its two-letter code; parsed from URL prefixes and query parameters.
/// </summary>
public readonly record struct Locale
{
  public static readonly Locale German = new("de");
  public static readonly Locale English = new("en");

  public static IReadOnlyList<Locale> All { get; } = [German, English];

  private Locale(string code) => Code = code;

  public string Code { get; }

  public bool IsPrimary => this == German;

  public static Locale Parse(string? code)
    => TryParse(code, out var locale)
      ? locale
      : throw new DomainValidationException("locale", $"Unsupported locale '{code}'. Supported: de, en.");

  public static bool TryParse(string? code, out Locale locale)
  {
    var normalized = code?.Trim().ToLowerInvariant();
    foreach (var candidate in All)
    {
      if (candidate.Code == normalized)
      {
        locale = candidate;
        return true;
      }
    }

    locale = default;
    return false;
  }

  public override string ToString() => Code;
}
