using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Interests.Application;
using SuracDev.Api.Domains.Interests.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Interests.Controllers;

/// <summary>The sections of the "Über mich" page in the admin area: list, read, create, replace and delete.</summary>
[ApiController]
[Route("api/admin/interests")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminInterestsController(InterestService interestService) : ControllerBase
{
  /// <summary>Lists every interest, including hidden ones, in page order.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All interests with both locales.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminInterest>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminInterest>> List(CancellationToken cancellationToken)
    => interestService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one interest for the editor.</summary>
  /// <param name="id">The interest's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The interest with both locales.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No interest has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminInterest>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminInterest> Get(long id, CancellationToken cancellationToken)
    => interestService.GetAdminAsync(id, cancellationToken);

  /// <summary>Creates an interest.</summary>
  /// <remarks>
  /// The German title and a known kind are required; <c>en: null</c> creates the interest without
  /// English texts, which then fall back to German. The image must be a public image document.
  /// </remarks>
  /// <param name="form">The new interest.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new interest.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>kind</c>, <c>imageDocumentId</c>, <c>de.title</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPost]
  [ProducesResponseType<AdminInterest>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AdminInterest>> Create(InterestForm form, CancellationToken cancellationToken)
  {
    var created = await interestService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces an interest with the given fields.</summary>
  /// <remarks>
  /// Full replacement: fields left out are cleared. <c>en: null</c> removes the English texts.
  /// </remarks>
  /// <param name="id">The interest's identifier.</param>
  /// <param name="form">The complete interest.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No interest has this id.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Update(long id, InterestForm form, CancellationToken cancellationToken)
  {
    await interestService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes an interest with its translations; its image stays in the documents.</summary>
  /// <remarks>To take an interest off the site without losing it, set <c>isVisible</c> to false instead.</remarks>
  /// <param name="id">The interest's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No interest has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await interestService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
