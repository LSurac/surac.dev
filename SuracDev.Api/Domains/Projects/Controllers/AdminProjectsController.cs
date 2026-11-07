using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Projects.Application;
using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Projects.Controllers;

/// <summary>Projects in the admin area: list, read, create, replace and delete, with decisions and images.</summary>
[ApiController]
[Route("api/admin/projects")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminProjectsController(ProjectService projectService) : ControllerBase
{
  /// <summary>Lists every project, including hidden ones, in display order.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All projects with both locales, decisions and images.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminProject>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminProject>> List(CancellationToken cancellationToken)
    => projectService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one project for the editor.</summary>
  /// <param name="id">The project's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The project with both locales, decisions and images.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No project has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<AdminProject>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminProject> Get(long id, CancellationToken cancellationToken)
    => projectService.GetAdminAsync(id, cancellationToken);

  /// <summary>Creates a project with its decisions and images.</summary>
  /// <remarks>
  /// German title and teaser are required; <c>en: null</c> creates the project without English
  /// texts. The slug must be unique. At most five visible projects can be featured at a time.
  /// Every image must reference a public image document (PNG, JPEG, WebP).
  /// </remarks>
  /// <param name="form">The new project.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Created; <c>Location</c> points to the new project.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field (<c>slug</c>, <c>status</c>, <c>de.title</c>, <c>images</c>).</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="409">The slug is already used, or five visible projects are already featured.</response>
  [HttpPost]
  [ProducesResponseType<AdminProject>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<ActionResult<AdminProject>> Create(ProjectForm form, CancellationToken cancellationToken)
  {
    var created = await projectService.CreateAsync(form, cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
  }

  /// <summary>Replaces a project, its decisions and its images.</summary>
  /// <remarks>
  /// Full replacement: decisions and images are replaced by the given lists in their order;
  /// <c>en: null</c> removes the English texts. Keeping the project's own slug is not a conflict.
  /// </remarks>
  /// <param name="id">The project's identifier.</param>
  /// <param name="form">The complete project.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule; the errors are keyed by field.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No project has this id.</response>
  /// <response code="409">The slug is used by another project, or five visible projects are already featured.</response>
  [HttpPut("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> Update(long id, ProjectForm form, CancellationToken cancellationToken)
  {
    await projectService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes a project with its texts, decisions and image references.</summary>
  /// <remarks>
  /// The image documents themselves stay; delete them separately. To take a project off the site
  /// without losing it, set <c>isVisible</c> to false instead.
  /// </remarks>
  /// <param name="id">The project's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No project has this id.</response>
  [HttpDelete("{id:long}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await projectService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }
}
