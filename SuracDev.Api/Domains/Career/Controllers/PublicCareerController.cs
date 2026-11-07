using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Career.Application;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Career.Controllers;

/// <summary>The career timeline of the open site: career stations and education.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicCareerController(ExperienceService experienceService, EducationService educationService)
  : ControllerBase
{
  /// <summary>Lists the visible career stations, newest first.</summary>
  /// <remarks>
  /// Ordered by start date descending, then by sort order. Hidden stations are never listed. Texts
  /// fall back to German field by field; each station's <c>fallbackFields</c> names the fields that did.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The visible stations; an empty list when there are none.</response>
  /// <response code="400">The locale is not supported.</response>
  [HttpGet("experiences")]
  [ProducesResponseType<IReadOnlyList<PublicExperience>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public Task<IReadOnlyList<PublicExperience>> ListExperiences(
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => experienceService.ListPublicAsync(Locale.Parse(locale), cancellationToken);

  /// <summary>Lists the visible education entries, newest first.</summary>
  /// <remarks>
  /// Ordered by start date descending, then by sort order. Hidden entries are never listed. Texts
  /// fall back to German field by field; each entry's <c>fallbackFields</c> names the fields that did.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The visible entries; an empty list when there are none.</response>
  /// <response code="400">The locale is not supported.</response>
  [HttpGet("education")]
  [ProducesResponseType<IReadOnlyList<PublicEducation>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public Task<IReadOnlyList<PublicEducation>> ListEducation(
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => educationService.ListPublicAsync(Locale.Parse(locale), cancellationToken);
}
