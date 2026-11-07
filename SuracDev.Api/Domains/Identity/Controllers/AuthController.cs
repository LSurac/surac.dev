using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuracDev.Api.Domains.Identity.Application;
using SuracDev.Api.Domains.Identity.Contracts;
using SuracDev.Api.Domains.Identity.Domain;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Identity.Controllers;

[ApiController]
[Route("api/auth")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
public sealed class AuthController(AdminAuthService authService) : ControllerBase
{
  [AllowAnonymous]
  [HttpGet("state")]
  public async Task<LoginStateResponse> State(CancellationToken cancellationToken)
  {
    var admin = await authService.GetAdminAsync(cancellationToken);
    return new LoginStateResponse(admin.HasPassword);
  }

  [AllowAnonymous]
  [EnableRateLimiting(RateLimitPolicies.Login)]
  [HttpPost("login")]
  public async Task<ActionResult<SessionResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
  {
    var hadPassword = (await authService.GetAdminAsync(cancellationToken)).HasPassword;
    var admin = await authService.LoginAsync(request.Password, cancellationToken);
    await SignInAsync(admin);
    return new SessionResponse(admin.Id, admin.DisplayName, IsInitialLogin: !hadPassword);
  }

  [HttpPost("logout")]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return NoContent();
  }

  [HttpGet("me")]
  public async Task<ActionResult<SessionResponse>> Me(CancellationToken cancellationToken)
  {
    var admin = await authService.GetByIdAsync(CurrentAdminId(), cancellationToken);
    return new SessionResponse(admin.Id, admin.DisplayName, IsInitialLogin: false);
  }

  [HttpPost("change-password")]
  public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
  {
    // The rotated stamp logs out other devices; re-issue this device's cookie so it stays signed in.
    var admin = await authService.ChangePasswordAsync(
      CurrentAdminId(), request.CurrentPassword, request.NewPassword, cancellationToken);
    await SignInAsync(admin);
    return NoContent();
  }

  private long CurrentAdminId()
    => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

  private Task SignInAsync(AdminUser admin)
  {
    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, admin.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
      new(ClaimTypes.Name, admin.DisplayName),
      new(SecurityStampValidator.SecurityStampClaim, admin.SecurityStamp.ToString()),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return HttpContext.SignInAsync(
      CookieAuthenticationDefaults.AuthenticationScheme,
      new ClaimsPrincipal(identity),
      new AuthenticationProperties { IsPersistent = true });
  }
}
