using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SuracDev.Api.Shared.Infrastructure.Telemetry;

/// <summary>
/// OpenTelemetry traces and logs over OTLP, enabled only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is
/// set. Local runs, tests and staging stay silent; production exports to the maintainer's Seq
/// (docs/ops-and-deploy.md, "Telemetry"). The exporter reads endpoint, protocol and headers from
/// the standard OTEL_* environment variables, so no secret touches configuration files.
/// </summary>
public static class TelemetryExtensions
{
  private const string ServiceName = "surac-dev-api";

  // Built into .NET 10 and Npgsql, no instrumentation packages needed: request duration and counts,
  // Kestrel connections, rate-limiter rejections, unhandled exceptions, sign-ins, outgoing HTTP,
  // GC/CPU/memory, database commands. The same list as the maintainer's other sites.
  private static readonly string[] FrameworkMeters =
  [
    "Microsoft.AspNetCore.Hosting",
    "Microsoft.AspNetCore.Server.Kestrel",
    "Microsoft.AspNetCore.RateLimiting",
    "Microsoft.AspNetCore.Diagnostics",
    "Microsoft.AspNetCore.Authentication",
    "System.Net.Http",
    "System.Runtime",
    "Npgsql",
  ];

  public static WebApplicationBuilder AddSuracDevTelemetry(this WebApplicationBuilder builder)
  {
    var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    if (string.IsNullOrWhiteSpace(endpoint))
    {
      return builder;
    }

    var resource = ResourceBuilder.CreateDefault().AddService(ServiceName);

    builder.Services.AddOpenTelemetry()
      .WithTracing(tracing => tracing
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation(options =>
        {
          // Health probes and static noise never become spans.
          options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/api/health");
          // Application-link tokens are capabilities and never reach the telemetry store.
          options.EnrichWithHttpRequest = (activity, request) =>
            activity.SetTag("url.path", TelemetryRedaction.Path(request.Path.Value ?? string.Empty));
        })
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql")
        .AddOtlpExporter())
      .WithMetrics(metrics => metrics
        .SetResourceBuilder(resource)
        .AddMeter(FrameworkMeters)
        .AddMeter(SiteMetrics.MeterName)
        .AddOtlpExporter((_, reader) => reader.TemporalityPreference = MetricReaderTemporalityPreference.Delta));

    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.SetResourceBuilder(resource);
      // No scopes: ASP.NET Core's request scope carries the raw RequestPath, which for /api/links/{token}
      // is the token itself, on every line logged during that request. Trace and span ids travel in the
      // OTLP log record anyway.
      logging.IncludeScopes = false;
      logging.IncludeFormattedMessage = true;
      logging.AddOtlpExporter();
    });

    return builder;
  }
}
