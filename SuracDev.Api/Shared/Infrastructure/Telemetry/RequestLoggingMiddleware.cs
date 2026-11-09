using System.Diagnostics;
using System.Security.Claims;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>
/// One log line per API request: method, path (never the query string, and with link tokens replaced),
/// status, duration, the admin or "anonymous", and the client address (<see cref="ClientAddress"/>).
/// Together with the page views (<see cref="PageViewsController"/>) it feeds the maintainer's Seq
/// dashboards, in the same shape as his other sites.
/// <para>
/// A browser's request comes through nginx's /api/ location, which sets X-Real-IP. The SSR process
/// calls the API directly: it passes the page request's X-Forwarded-For on for the rate limits, but no
/// X-Real-IP, so its calls are logged at Debug only; the page view already covers them. Registered
/// before <c>UseForwardedHeaders</c>; address and user are read after the rest of the pipeline ran, so
/// they are the real ones.
/// </para>
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
  // Health checks every minute and the page-view beacon, which logs a line of its own.
  private static readonly string[] QuietPathPrefixes = ["/api/health", "/" + PageViewsController.Route];

  public async Task InvokeAsync(HttpContext context)
  {
    var cameThroughProxy = context.Request.Headers.ContainsKey("X-Real-IP");
    var startedAt = Stopwatch.GetTimestamp();
    try
    {
      await next(context);
    }
    finally
    {
      Log(context, cameThroughProxy, Stopwatch.GetElapsedTime(startedAt));
    }
  }

  // Request.Path is decoded: %0A arrives as a real line feed and would forge a line in the console log.
  private static string ForLog(string path)
    => TelemetryRedaction.Path(string.Concat(path.Select(character => char.IsControl(character) ? '_' : character)));

  private void Log(HttpContext context, bool cameThroughProxy, TimeSpan elapsed)
  {
    var path = context.Request.Path.Value ?? string.Empty;
    if (QuietPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
    {
      return;
    }

    var visitor = Visitor.Of(context);
    Activity.Current?.SetTag("enduser.id", visitor.Name);
    Activity.Current?.SetTag("enduser.role", visitor.Role);

    logger.Log(
      cameThroughProxy ? LogLevel.Information : LogLevel.Debug,
      "{RequestMethod} {RequestPath} answered {StatusCode} in {ElapsedMs} ms for {UserName} ({UserRole}) from {ClientIp}.",
      context.Request.Method,
      ForLog(path),
      context.Response.StatusCode,
      Math.Round(elapsed.TotalMilliseconds),
      visitor.Name,
      visitor.Role,
      visitor.ClientIp);
  }
}

/// <summary>Who a request came from, in the shape the request and page-view log lines use.</summary>
public sealed record Visitor(string Name, string Role, string ClientIp)
{
  public const string AnonymousName = "anonymous";

  /// <summary>The only role there is: the single admin (docs/security-and-privacy.md).</summary>
  public const string AdminRole = "admin";

  public static Visitor Of(HttpContext context)
  {
    var isSignedIn = context.User.Identity?.IsAuthenticated == true;
    return new Visitor(
      isSignedIn ? context.User.FindFirstValue(ClaimTypes.Name) ?? "unknown" : AnonymousName,
      isSignedIn ? AdminRole : "-",
      ClientAddress.ForLog(context.Connection.RemoteIpAddress, keepFullAddress: isSignedIn));
  }
}
