using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Applications.Controllers;

/// <summary>The links of one application in the admin area: list, read, create, change and revoke.</summary>
[ApiController]
[Route("api/admin/applications/{applicationId:long}/links")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminApplicationLinksController(ApplicationLinkService linkService) : ControllerBase
{
  /// <summary>Lists the links of an application, newest first.</summary>
  /// <remarks>The clear tokens are not part of the list; they exist only in the response that created a link.</remarks>
  /// <param name="applicationId">The application's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The links.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No application has this id.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminApplicationLink>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<IReadOnlyList<AdminApplicationLink>> List(long applicationId, CancellationToken cancellationToken)
    => linkService.ListAsync(applicationId, cancellationToken);

  /// <summary>Reads one link of an application.</summary>
  /// <param name="applicationId">The application's identifier.</param>
  /// <param name="linkId">The link's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The link, without its token.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">The application has no link with this id.</response>
  [HttpGet("{linkId:long}")]
  [ProducesResponseType<AdminApplicationLink>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminApplicationLink> Get(long applicationId, long linkId, CancellationToken cancellationToken)
    => linkService.GetAsync(applicationId, linkId, cancellationToken);

  /// <summary>Creates a link and returns its token once.</summary>
  /// <remarks>
  /// The token is 128 random bits in base64url; the link's public path is <c>/b/{token}</c>. Only
  /// its SHA-256 is stored, so this response is the only chance to copy it. Without
  /// <c>expiresAt</c> the link expires after 90 days. Only link-only documents can be attached.
  /// </remarks>
  /// <param name="applicationId">The application's identifier.</param>
  /// <param name="form">The link's settings.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; the body carries the token, <c>Location</c> points to the link.</response>
  /// <response code="400">A setting breaks a rule (<c>profile</c>, <c>expiresAt</c>, <c>greetingDe</c>,
  /// <c>documentIds</c>).</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No application has this id.</response>
  [HttpPost]
  [ProducesResponseType<CreatedApplicationLink>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<CreatedApplicationLink>> Create(
    long applicationId,
    ApplicationLinkForm form,
    CancellationToken cancellationToken)
  {
    var created = await linkService.CreateAsync(applicationId, form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { applicationId, linkId = created.Link.Id }, created);
  }

  /// <summary>Replaces the settings and documents of a link.</summary>
  /// <remarks>The token stays the same. A revoked link cannot be changed; create a new one instead.</remarks>
  /// <param name="applicationId">The application's identifier.</param>
  /// <param name="linkId">The link's identifier.</param>
  /// <param name="form">The complete settings.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A setting breaks a rule.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">The application has no link with this id.</response>
  /// <response code="409">The link is revoked.</response>
  [HttpPut("{linkId:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> Update(
    long applicationId,
    long linkId,
    ApplicationLinkForm form,
    CancellationToken cancellationToken)
  {
    await linkService.UpdateAsync(applicationId, linkId, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Revokes a link for good.</summary>
  /// <remarks>
  /// From now on the link answers 404 like one that never existed. Revoking is final and
  /// idempotent; the link and its statistics stay visible in the admin area.
  /// </remarks>
  /// <param name="applicationId">The application's identifier.</param>
  /// <param name="linkId">The link's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Revoked.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">The application has no link with this id.</response>
  [HttpPost("{linkId:long}/revoke")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Revoke(long applicationId, long linkId, CancellationToken cancellationToken)
  {
    await linkService.RevokeAsync(applicationId, linkId, cancellationToken);
    return NoContent();
  }
}
