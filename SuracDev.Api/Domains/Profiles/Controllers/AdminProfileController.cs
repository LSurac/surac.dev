using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Profiles.Application;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Profiles.Controllers;

/// <summary>
/// The profile in the admin area: contact data, both locales side by side, fact bar and photo.
/// The profile is a singleton, so there is no collection and no create or delete.
/// </summary>
[ApiController]
[Route("api/admin/profile")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminProfileController(ProfileService profileService) : ControllerBase
{
  /// <summary>Reads the complete profile for the editor, including phone and street address.</summary>
  /// <remarks>
  /// <c>de</c> always carries the German texts. <c>en</c> is null when no English text exists yet;
  /// a single empty English field means that field falls back to German on the open site.
  /// </remarks>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The profile with both locales.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No profile exists yet.</response>
  [HttpGet]
  [ProducesResponseType<AdminProfile>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminProfile> Get(CancellationToken cancellationToken)
    => profileService.GetAdminAsync(cancellationToken);

  /// <summary>Replaces the complete profile with the given one.</summary>
  /// <remarks>
  /// Full replacement: send everything <c>GET</c> returned, changed where needed. German title line
  /// and headline are required; <c>en: null</c> or English fields left empty remove the English
  /// texts, which then fall back to German. The fact bar is replaced in the given order (at most
  /// four cells). <c>photoDocumentId</c> must name a public image document or be null.
  /// </remarks>
  /// <param name="profile">The complete profile.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>de.headline</c>, <c>facts</c>, <c>photoDocumentId</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPut]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Put(AdminProfile profile, CancellationToken cancellationToken)
  {
    await profileService.SaveAsync(profile, cancellationToken);
    return NoContent();
  }
}
