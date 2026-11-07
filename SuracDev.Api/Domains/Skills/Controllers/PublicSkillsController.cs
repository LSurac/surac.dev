using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Skills.Application;
using SuracDev.Api.Domains.Skills.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Skills.Controllers;

/// <summary>The skills section of the open site.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/skills")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicSkillsController(SkillService skillService) : ControllerBase
{
  /// <summary>Lists the skill groups with their skills, in display order.</summary>
  /// <remarks>
  /// A skill carries the year since it is used and an optional qualifier ("Legacy"), never a level
  /// or a percentage. Group names and qualifiers fall back to German; a group's
  /// <c>fallbackFields</c> names its own fields that did.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The groups; an empty list when there are none.</response>
  /// <response code="400">The locale is not supported.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<PublicSkillGroup>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public Task<IReadOnlyList<PublicSkillGroup>> List(
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => skillService.ListPublicAsync(Locale.Parse(locale), cancellationToken);
}
