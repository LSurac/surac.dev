using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Profiles.Application;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Profiles.Controllers;

/// <summary>The person behind the site, as the open site shows it.</summary>
[ApiController]
[AllowAnonymous]
[AnswersHead]
[Route("api/public/profile")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicProfileController(ProfileService profileService) : ControllerBase
{
  /// <summary>Reads the profile for the hero, the fact bar and the contact section.</summary>
  /// <remarks>
  /// Texts come in the requested locale and fall back to German field by field; <c>fallbackFields</c>
  /// lists the fields that did, so the page can decide to hide an untranslated section. Phone number
  /// and street address are never part of this response; they exist only in the full CV PDF and,
  /// for the address, the Impressum.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The profile in the requested locale.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No profile exists yet.</response>
  [HttpGet]
  [ProducesResponseType<PublicProfile>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<PublicProfile> Get([FromQuery] string locale = "de", CancellationToken cancellationToken = default)
    => profileService.GetPublicAsync(Locale.Parse(locale), cancellationToken);
}
