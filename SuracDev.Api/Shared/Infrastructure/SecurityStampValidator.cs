using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;

namespace SuracDev.Api.Shared.Infrastructure;

/// <summary>
/// Validates the auth cookie against the admin's current security stamp on every request. A
/// password change rotates the stamp, so every previously issued cookie fails here and forces a
/// new login on all other devices.
/// </summary>
public static class SecurityStampValidator
{
  public const string SecurityStampClaim = "security_stamp";

  public static async Task ValidateAsync(CookieValidatePrincipalContext context)
  {
    var principal = context.Principal;
    var idValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    var stampValue = principal?.FindFirstValue(SecurityStampClaim);

    if (!long.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var adminId)
      || string.IsNullOrEmpty(stampValue))
    {
      await RejectAsync(context);
      return;
    }

    var dbContext = context.HttpContext.RequestServices.GetRequiredService<SuracDevDbContext>();
    var currentStamp = await dbContext.AdminUsers
      .Where(user => user.Id == adminId)
      .Select(user => (Guid?)user.SecurityStamp)
      .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

    if (currentStamp is null || currentStamp.Value.ToString() != stampValue)
    {
      await RejectAsync(context);
    }
  }

  private static async Task RejectAsync(CookieValidatePrincipalContext context)
  {
    context.RejectPrincipal();
    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
  }
}
