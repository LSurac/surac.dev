using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Applications.Controllers;

/// <summary>Applications in the admin area, the maintainer's mini ATS: list, read, create, replace and delete.</summary>
[ApiController]
[Route("api/admin/applications")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminApplicationsController(ApplicationService applicationService) : ControllerBase
{
  /// <summary>Lists every application, newest first.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All applications.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminApplication>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminApplication>> List(CancellationToken cancellationToken)
    => applicationService.ListAsync(cancellationToken);

  /// <summary>Reads one application.</summary>
  /// <param name="id">The application's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The application.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No application has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminApplication>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminApplication> Get(long id, CancellationToken cancellationToken)
    => applicationService.GetAsync(id, cancellationToken);

  /// <summary>Creates an application.</summary>
  /// <remarks>Company, position and a known status are required; the job ad must be an absolute http(s) URL.</remarks>
  /// <param name="form">The new application.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new application.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>company</c>, <c>status</c>, <c>jobAdUrl</c>).</response>
  /// <response code="401">Not logged in.</response>
  [HttpPost]
  [ProducesResponseType<AdminApplication>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AdminApplication>> Create(ApplicationForm form, CancellationToken cancellationToken)
  {
    var created = await applicationService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces an application with the given fields.</summary>
  /// <remarks>Full replacement: fields left out are cleared. The links of the application are not touched.</remarks>
  /// <param name="id">The application's identifier.</param>
  /// <param name="form">The complete application.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No application has this id.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Update(long id, ApplicationForm form, CancellationToken cancellationToken)
  {
    await applicationService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes an application with its links and their statistics.</summary>
  /// <remarks>The links stop working at once. The attached documents stay; they belong to the documents area.</remarks>
  /// <param name="id">The application's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No application has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await applicationService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
