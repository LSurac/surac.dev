using System.Diagnostics.Metrics;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>
/// The site's own counters next to the framework meters. Tags stay low-cardinality on purpose (an
/// area, an outcome), so a metric never names a page or a person; per-page questions are answered from
/// the log events. Meter and counter names follow the maintainer's other sites, so the Seq dashboards
/// take the prefix as their only difference.
/// </summary>
public static class SiteMetrics
{
  public const string MeterName = "SuracDev.Site";

  private static readonly Meter Meter = new(MeterName);

  private static readonly Counter<long> PageViews =
    Meter.CreateCounter<long>("suracdev.page_views", description: "Pages shown in the browser.");

  private static readonly Counter<long> Logins =
    Meter.CreateCounter<long>("suracdev.logins", description: "Login attempts by outcome.");

  public static void RecordPageView(string path, bool isSignedIn)
    => PageViews.Add(1, new("area", AreaOf(path)), new("signed_in", isSignedIn));

  /// <summary><paramref name="outcome"/> is "succeeded" or a failure reason.</summary>
  public static void RecordLogin(string outcome)
    => Logins.Add(1, new("outcome", outcome), new("page", "admin"));

  /// <summary>"admin", "link" (an application link) or "public": the part of the site a path belongs to.</summary>
  public static string AreaOf(string path)
  {
    if (path == "/admin" || path.StartsWith("/admin/", StringComparison.Ordinal))
    {
      return "admin";
    }

    return path.StartsWith("/b/", StringComparison.Ordinal) ? "link" : "public";
  }
}
