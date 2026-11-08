using OpenTelemetry.Logs;
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
        .AddOtlpExporter());

    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.SetResourceBuilder(resource);
      logging.IncludeScopes = true;
      logging.AddOtlpExporter();
    });

    return builder;
  }
}
