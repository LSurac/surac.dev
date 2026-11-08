using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Career.Application;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Career.Controllers;

/// <summary>Career stations in the admin area: list, read, create, replace and delete.</summary>
[ApiController]
[Route("api/admin/experiences")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminExperiencesController(ExperienceService experienceService) : ControllerBase
{
  /// <summary>Lists every career station, including hidden ones, newest first.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All stations with both locales.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminExperience>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminExperience>> List(CancellationToken cancellationToken)
    => experienceService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one career station for the editor.</summary>
  /// <param name="id">The station's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The station with both locales.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No station has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminExperience>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminExperience> Get(long id, CancellationToken cancellationToken)
    => experienceService.GetAdminAsync(id, cancellationToken);

  /// <summary>Creates a career station.</summary>
  /// <remarks>
  /// The German role title is required; <c>en: null</c> creates the station without English texts,
  /// which then fall back to German. An end date before the start date is rejected; a null end
  /// date marks the current station.
  /// </remarks>
  /// <param name="form">The new station.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new station.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>company</c>, <c>endDate</c>,
  /// <c>de.roleTitle</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPost]
  [ProducesResponseType<AdminExperience>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AdminExperience>> Create(ExperienceForm form, CancellationToken cancellationToken)
  {
    var created = await experienceService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces a career station with the given fields.</summary>
  /// <remarks>
  /// Full replacement: fields left out are cleared. <c>en: null</c> removes the English texts.
  /// </remarks>
  /// <param name="id">The station's identifier.</param>
  /// <param name="form">The complete station.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No station has this id.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Update(long id, ExperienceForm form, CancellationToken cancellationToken)
  {
    await experienceService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes a career station with its translations.</summary>
  /// <remarks>To take a station off the site without losing it, set <c>isVisible</c> to false instead.</remarks>
  /// <param name="id">The station's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No station has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await experienceService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
