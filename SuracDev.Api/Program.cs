using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Identity.Application;
using SuracDev.Api.Domains.Profiles.Application;
using SuracDev.Api.Shared.Infrastructure;
using SuracDev.Api.Shared.Infrastructure.Seeding;
using SuracDev.Api.Shared.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// The "OpenApi" environment generates the NSwag documents without a database: it skips the
// connection-string requirement, the migrations and the seeding (docs/gotchas.md).
var isOpenApiGeneration = builder.Environment.IsEnvironment("OpenApi");

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString) && !isOpenApiGeneration)
{
  throw new InvalidOperationException(
    "Connection string 'Default' is required. Configure ConnectionStrings__Default.");
}

builder.Services.AddDbContext<SuracDevDbContext>(options =>
  options.UseNpgsql(
    connectionString ?? "Host=localhost;Database=suracdev_openapi;Username=openapi;Password=openapi",
    npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SuracDevSeeder>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<DocumentReferences>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddSingleton(TimeProvider.System);

// Behind the web container's nginx (staging) or Caddy → nginx (production) the client address only
// arrives in X-Forwarded-For. The API is never published directly, so every peer is a trusted proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownIPNetworks.Clear();
  options.KnownProxies.Clear();
});

// The Data Protection key ring encrypts the auth cookie. Persisting it keeps logins valid across
// container rebuilds; without a path (local dev, OpenApi, tests) the keys stay in memory.
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("surac.dev");
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
  dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));
}

builder.Services
  .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
    options.Cookie.Name = "suracdev.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Staging has no TLS; production always arrives as HTTPS through Caddy (X-Forwarded-Proto).
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = SecurityStampValidator.ValidateAsync;
    // API semantics: status codes instead of redirects to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
      context.Response.StatusCode = StatusCodes.Status403Forbidden;
      return Task.CompletedTask;
    };
  });

// Deny by default: every endpoint requires the admin unless it opts out with [AllowAnonymous].
// docs/security-and-privacy.md lists the endpoints that do; AuthorizationSweepTests enforces it.
builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
  options.AddPolicy(RateLimitPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    _ => new FixedWindowRateLimiterOptions
    {
      PermitLimit = 10,
      Window = TimeSpan.FromMinutes(15),
      QueueLimit = 0,
    }));
});

builder.Services
  .AddControllers()
  .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Two documents, two generated clients: admin code never reaches the public bundle.
builder.Services.AddOpenApiDocument(settings =>
{
  settings.DocumentName = ApiGroups.Public;
  settings.ApiGroupNames = [ApiGroups.Public];
  settings.Title = "surac.dev public API";
});
builder.Services.AddOpenApiDocument(settings =>
{
  settings.DocumentName = ApiGroups.Admin;
  settings.ApiGroupNames = [ApiGroups.Admin];
  settings.Title = "surac.dev admin API";
});

builder.AddSuracDevTelemetry();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
  app.UseOpenApi();
  app.UseSwaggerUi();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!isOpenApiGeneration)
{
  await using var scope = app.Services.CreateAsyncScope();
  var dbContext = scope.ServiceProvider.GetRequiredService<SuracDevDbContext>();
  await dbContext.Database.MigrateAsync();

  var seeder = scope.ServiceProvider.GetRequiredService<SuracDevSeeder>();
  await seeder.SeedAsync();
}

app.Run();

/// <summary>Exposes the entry point to <c>WebApplicationFactory</c> in the integration tests.</summary>
public partial class Program;
