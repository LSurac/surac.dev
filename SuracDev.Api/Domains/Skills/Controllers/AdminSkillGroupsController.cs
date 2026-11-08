using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Skills.Application;
using SuracDev.Api.Domains.Skills.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Skills.Controllers;

/// <summary>
/// Skill groups in the admin area. A group owns its skills, so skills are created, reordered and
/// removed by replacing the group.
/// </summary>
[ApiController]
[Route("api/admin/skill-groups")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminSkillGroupsController(SkillService skillService) : ControllerBase
{
  /// <summary>Lists every skill group with its skills, in display order.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All groups with both names and their skills.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminSkillGroup>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminSkillGroup>> List(CancellationToken cancellationToken)
    => skillService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one skill group for the editor.</summary>
  /// <param name="id">The group's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The group with both names and its skills.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No group has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminSkillGroup>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminSkillGroup> Get(long id, CancellationToken cancellationToken)
    => skillService.GetAdminAsync(id, cancellationToken);

  /// <summary>Creates a skill group with its skills.</summary>
  /// <remarks>
  /// The German name is required. Skills keep the given order; a name may appear only once per
  /// group (ignoring case), and a "since year" must lie between 1950 and 2100.
  /// </remarks>
  /// <param name="form">The new group.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new group.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>de.name</c>, <c>skills</c>,
  /// <c>skills.sinceYear</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPost]
  [ProducesResponseType<AdminSkillGroup>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AdminSkillGroup>> Create(SkillGroupForm form, CancellationToken cancellationToken)
  {
    var created = await skillService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces a skill group's names, position and skills.</summary>
  /// <remarks>Full replacement: the skills are replaced by the given list in its order.</remarks>
  /// <param name="id">The group's identifier.</param>
  /// <param name="form">The complete group.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No group has this id.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Update(long id, SkillGroupForm form, CancellationToken cancellationToken)
  {
    await skillService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes a skill group with all its skills.</summary>
  /// <param name="id">The group's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No group has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await skillService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
