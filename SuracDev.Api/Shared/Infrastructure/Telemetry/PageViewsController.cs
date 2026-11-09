using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>One page the browser showed.</summary>
/// <param name="Path">The site path without query string or fragment; link pages arrive as <c>/b/{token}</c>.</param>
/// <param name="ReferrerHost">Host of the external page the visit came from (first page of a visit only), never a URL.</param>
public sealed record PageViewRequest(string Path, string? ReferrerHost = null);

/// <summary>
/// The browser reports every page it shows here (<c>PageViewTracker</c> in the web app), because after
/// the first page an Angular route change only fetches data and leaves no trace of which page was
/// opened. Nothing is stored: the request becomes one log line. Anonymous on purpose, with its own rate
/// limit so a script cannot flood the log. Left out of the OpenAPI documents: the browser calls it with
/// sendBeacon, not through a generated client.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route(Route)]
[AllowAnonymous]
[ResponseCache(NoStore = true)]
public sealed partial class PageViewsController(ILogger<PageViewsController> logger) : ControllerBase
{
  public const string Route = "api/public/page-views";
  private const int MaxPathLength = 300;

  [HttpPost]
  [EnableRateLimiting(RateLimitPolicies.PageViews)]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  public IActionResult Record(PageViewRequest request)
  {
    if (!IsSitePath(request.Path))
    {
      throw new DomainValidationException("path", "Must be a site path without query string, at most 300 characters.");
    }

    if (request.ReferrerHost is not null && !HostPattern().IsMatch(request.ReferrerHost))
    {
      throw new DomainValidationException("referrerHost", "Must be a host name, at most 100 characters.");
    }

    // The browser already sends /b/{token}; the server does not rely on it.
    var path = TelemetryRedaction.Path(request.Path);
    var visitor = Visitor.Of(HttpContext);
    SiteMetrics.RecordPageView(path, isSignedIn: visitor.Name != Visitor.AnonymousName);
    logger.LogInformation(
      "Page view {PagePath} ({PageArea}) by {UserName} ({UserRole}) from {ClientIp} via {ReferrerHost}.",
      path,
      SiteMetrics.AreaOf(path),
      visitor.Name,
      visitor.Role,
      visitor.ClientIp,
      request.ReferrerHost ?? "-");
    return NoContent();
  }

  [GeneratedRegex(@"^[a-z0-9]([a-z0-9.-]{0,98}[a-z0-9])?\z")]
  private static partial Regex HostPattern();

  // A query string or fragment could carry a token; control characters would let a client forge log lines.
  private static bool IsSitePath(string? path)
    => path is { Length: > 0 and <= MaxPathLength }
      && path[0] == '/'
      && !path.Any(character => character is '?' or '#' || char.IsControl(character));
}
