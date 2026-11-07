using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuracDev.Api.Data;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// The stamp-mismatch path is covered end to end by <c>AuthEndpointTests</c> (password change logs
/// out the other device). These tests cover the malformed-cookie paths, which no API call can
/// produce: a principal without the id or stamp claim is rejected before any database access.
/// </summary>
public sealed class SecurityStampValidatorTests
{
  [Fact]
  public async Task A_principal_without_an_id_claim_is_rejected()
  {
    var context = CreateContext(new Claim(SecurityStampValidator.SecurityStampClaim, Guid.NewGuid().ToString()));

    await SecurityStampValidator.ValidateAsync(context);

    Assert.Null(context.Principal);
  }

  [Fact]
  public async Task A_principal_without_a_stamp_claim_is_rejected()
  {
    var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, "1"));

    await SecurityStampValidator.ValidateAsync(context);

    Assert.Null(context.Principal);
  }

  private static CookieValidatePrincipalContext CreateContext(params Claim[] claims)
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDataProtection();
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
    // Never reached by these tests: the claims check rejects first.
    services.AddDbContext<SuracDevDbContext>(options => options.UseNpgsql("Host=localhost;Database=never;Username=never;Password=never"));
    var provider = services.BuildServiceProvider();

    var httpContext = new DefaultHttpContext { RequestServices = provider.CreateScope().ServiceProvider };
    var scheme = new AuthenticationScheme(
      CookieAuthenticationDefaults.AuthenticationScheme,
      displayName: null,
      typeof(CookieAuthenticationHandler));
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);

    return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
  }
}
