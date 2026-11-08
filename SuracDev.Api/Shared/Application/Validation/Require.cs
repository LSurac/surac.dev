using System.Net.Mail;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Shared.Application.Validation;

/// <summary>Maximum lengths shared by the domain rules and the column definitions.</summary>
public static class TextLimits
{
  public const int Name = 200;
  public const int Line = 500;
  public const int Url = 500;
  public const int Paragraph = 4_000;
  public const int Markdown = 100_000;
}

/// <summary>
/// Input rules every domain shares: trimmed text with limits, URLs, e-mail, years, date ranges and
/// the enum <c>Unknown</c> sentinel. Violations throw <see cref="DomainValidationException"/> keyed
/// by the JSON field name, so the admin can show them next to the input.
/// </summary>
public static class Require
{
  /// <summary>Earliest plausible year for career and project data.</summary>
  public const int MinYear = 1950;

  /// <summary>Latest plausible year for career and project data.</summary>
  public const int MaxYear = 2100;

  /// <summary>Trimmed, non-blank text within <paramref name="maxLength"/>.</summary>
  public static string Text(string? value, string field, int maxLength)
  {
    var trimmed = value?.Trim();
    if (string.IsNullOrEmpty(trimmed))
    {
      throw new DomainValidationException(field, "Required.");
    }

    return WithinLimit(trimmed, field, maxLength);
  }

  /// <summary>Trimmed text within <paramref name="maxLength"/>; blank input becomes <c>null</c>.</summary>
  public static string? OptionalText(string? value, string field, int maxLength)
  {
    var trimmed = value?.Trim();
    return string.IsNullOrEmpty(trimmed) ? null : WithinLimit(trimmed, field, maxLength);
  }

  /// <summary>A field of a translation: keyed as <c>de.title</c>, required only in German.</summary>
  public static string? Translated(string? value, Locale locale, string field, int maxLength, bool required = false)
  {
    var key = $"{locale.Code}.{field}";
    return required && locale.IsPrimary ? Text(value, key, maxLength) : OptionalText(value, key, maxLength);
  }

  /// <summary>An absolute http(s) URL or <c>null</c>; other schemes (<c>javascript:</c>, <c>ftp:</c>) are
  /// rejected.</summary>
  public static string? OptionalUrl(string? value, string field)
  {
    var text = OptionalText(value, field, TextLimits.Url);
    if (text is null)
    {
      return null;
    }

    var isWebUrl = Uri.TryCreate(text, UriKind.Absolute, out var uri)
      && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    return isWebUrl ? text : throw new DomainValidationException(field, "Must be an absolute http(s) URL.");
  }

  /// <summary>A required, single e-mail address without display name.</summary>
  public static string Email(string? value, string field)
  {
    var text = Text(value, field, TextLimits.Name);
    return MailAddress.TryCreate(text, out var address) && address.Address == text
      ? text
      : throw new DomainValidationException(field, "Must be an e-mail address.");
  }

  /// <summary>An ordered list of short texts (bullets, tags): trimmed, blanks dropped, limited.</summary>
  public static string[] Lines(IEnumerable<string?>? values, string field, int maxLength, int maxCount)
  {
    var lines = (values ?? [])
      .Select(value => OptionalText(value, field, maxLength))
      .OfType<string>()
      .ToArray();
    return lines.Length <= maxCount
      ? lines
      : throw new DomainValidationException(field, $"At most {maxCount} entries.");
  }

  /// <summary>Rejects an end date before the start date; an open end is allowed.</summary>
  public static void DateRange(DateOnly start, DateOnly? end)
  {
    if (end < start)
    {
      throw new DomainValidationException("endDate", "Must not be before the start date.");
    }
  }

  /// <summary>An optional year between <see cref="MinYear"/> and <see cref="MaxYear"/>.</summary>
  public static int? Year(int? year, string field)
    => year is null or (>= MinYear and <= MaxYear)
      ? year
      : throw new DomainValidationException(field, $"Must be between {MinYear} and {MaxYear}.");

  /// <summary>Enums are stored as int; <c>0</c> is the fail-loud <c>Unknown</c> and is never written.</summary>
  public static TEnum Known<TEnum>(TEnum value, string field)
    where TEnum : struct, Enum
    => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) != 0 && Enum.IsDefined(value)
      ? value
      : throw new DomainValidationException(field, $"Unknown value '{value}'.");

  private static string WithinLimit(string value, string field, int maxLength)
    => value.Length <= maxLength
      ? value
      : throw new DomainValidationException(field, $"At most {maxLength} characters.");
}
