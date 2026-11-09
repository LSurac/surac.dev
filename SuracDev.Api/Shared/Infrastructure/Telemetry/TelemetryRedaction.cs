using System.Text.RegularExpressions;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>
/// Removes application-link tokens from what telemetry records. A token is a capability (whoever
/// has it opens the link), so it must not end up in the telemetry store next to everything else
/// (docs/security-and-privacy.md).
/// </summary>
public static partial class TelemetryRedaction
{
  /// <summary>
  /// The path with the token segment of an API link (<c>/api/links/{token}</c>) or a link page
  /// (<c>/b/{token}</c>) replaced by a placeholder.
  /// </summary>
  public static string Path(string path)
    => PageTokenPattern().Replace(LinkTokenPattern().Replace(path, "/api/links/{token}"), "/b/{token}");

  [GeneratedRegex(@"^/api/links/[^/?#]+", RegexOptions.IgnoreCase)]
  private static partial Regex LinkTokenPattern();

  [GeneratedRegex(@"^/b/[^/?#]+", RegexOptions.IgnoreCase)]
  private static partial Regex PageTokenPattern();
}
