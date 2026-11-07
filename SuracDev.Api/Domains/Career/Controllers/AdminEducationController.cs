using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Career.Application;
using SuracDev.Api.Domains.Career.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Career.Controllers;

/// <summary>Education entries in the admin area: list, read, create, replace and delete.</summary>
[ApiController]
[Route("api/admin/education")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminEducationController(EducationService educationService) : ControllerBase
{
  /// <summary>Lists every education entry, including hidden ones, newest first.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All entries with both locales.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminEducation>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminEducation>> List(CancellationToken cancellationToken)
    => educationService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one education entry for the editor.</summary>
  /// <param name="id">The entry's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The entry with both locales.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No entry has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminEducation>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminEducation> Get(long id, CancellationToken cancellationToken)
    => educationService.GetAdminAsync(id, cancellationToken);

  /// <summary>Creates an education entry.</summary>
  /// <remarks>
  /// The German degree is required; <c>en: null</c> creates the entry without English texts. The
  /// grade is free text ("1,8", "gut") of at most 40 characters.
  /// </remarks>
  /// <param name="form">The new entry.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new entry.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>institution</c>, <c>endDate</c>, <c>de.degree</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPost]
  [ProducesResponseType<AdminEducation>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AdminEducation>> Create(EducationForm form, CancellationToken cancellationToken)
  {
    var created = await educationService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces an education entry with the given fields.</summary>
  /// <remarks>Full replacement: fields left out are cleared. <c>en: null</c> removes the English texts.</remarks>
  /// <param name="id">The entry's identifier.</param>
  /// <param name="form">The complete entry.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No entry has this id.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Update(long id, EducationForm form, CancellationToken cancellationToken)
  {
    await educationService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes an education entry with its translations.</summary>
  /// <remarks>To take an entry off the site without losing it, set <c>isVisible</c> to false instead.</remarks>
  /// <param name="id">The entry's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No entry has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await educationService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
