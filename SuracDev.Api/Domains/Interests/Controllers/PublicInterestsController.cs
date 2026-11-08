using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Interests.Application;
using SuracDev.Api.Domains.Interests.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Interests.Controllers;

/// <summary>The sections of the "Über mich" page of the open site.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/interests")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicInterestsController(InterestService interestService) : ControllerBase
{
  /// <summary>Lists the visible interests in page order.</summary>
  /// <remarks>
  /// Ordered by sort order, then by creation. Hidden interests are never listed. The body is
  /// rendered from the restricted Markdown to HTML. Texts fall back to German field by field; each
  /// interest's <c>fallbackFields</c> names the fields that did.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The visible interests; an empty list when there are none.</response>
  /// <response code="400">The locale is not supported.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<PublicInterest>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public Task<IReadOnlyList<PublicInterest>> List(
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => interestService.ListPublicAsync(Locale.Parse(locale), cancellationToken);
}
