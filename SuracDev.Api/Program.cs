using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Career.Application;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Identity.Application;
using SuracDev.Api.Domains.Interests.Application;
using SuracDev.Api.Domains.Legal.Application;
using SuracDev.Api.Domains.Pdf.Application;
using SuracDev.Api.Domains.Profiles.Application;
using SuracDev.Api.Domains.Projects.Application;
using SuracDev.Api.Domains.Skills.Application;
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
    npgsql => npgsql.EnableRetryOnFailure(
      maxRetryCount: 5,
      maxRetryDelay: TimeSpan.FromSeconds(10),
      errorCodesToAdd: null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SuracDevSeeder>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<DocumentReferences>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddSingleton<DocumentStorage>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<ExperienceService>();
builder.Services.AddScoped<EducationService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<InterestService>();
builder.Services.AddScoped<LegalPageService>();
builder.Services.AddScoped<CvService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<ApplicationLinkService>();
builder.Services.AddScoped<LinkAccessService>();
builder.Services.AddScoped<ApplicationStatisticsService>();

// The weekly digest of applications without reply; off unless an ntfy server and topic are configured.
var reminderSection = builder.Configuration.GetSection(ReminderOptions.Section);
builder.Services.Configure<ReminderOptions>(reminderSection);
builder.Services.AddHttpClient(ApplicationReminderService.HttpClientName);
builder.Services.AddScoped<ApplicationReminderService>();
if (reminderSection.Get<ReminderOptions>()?.IsEnabled == true && !isOpenApiGeneration)
{
  builder.Services.AddHostedService<ApplicationReminderWorker>();
}
builder.Services.AddSingleton(TimeProvider.System);

// The client address arrives in X-Forwarded-For: from nginx on staging, from Caddy → nginx in
// production, where nginx appends the Docker gateway Caddy connects through. The chain is read
// from the right for as long as the hop is one of ours (loopback or a Docker network); the first
// other address is the client. A header forged by a client therefore never gets past its own,
// untrusted address. Rate limits partition by the result (docs/security-and-privacy.md).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.ForwardLimit = null;
  options.KnownIPNetworks.Clear();
  options.KnownProxies.Clear();
  options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
  options.KnownProxies.Add(IPAddress.Loopback);
  options.KnownProxies.Add(IPAddress.IPv6Loopback);
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
  options.AddPolicy(RateLimitPolicies.Link, httpContext => RateLimitPartition.GetFixedWindowLimiter(
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    _ => new FixedWindowRateLimiterOptions
    {
      PermitLimit = 30,
      Window = TimeSpan.FromMinutes(1),
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
