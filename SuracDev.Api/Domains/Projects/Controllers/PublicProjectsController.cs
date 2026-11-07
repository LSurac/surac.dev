using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Projects.Application;
using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Projects.Controllers;

/// <summary>Project cards and case studies of the open site.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/projects")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicProjectsController(ProjectService projectService) : ControllerBase
{
  /// <summary>Lists the visible projects as cards, in display order.</summary>
  /// <remarks>
  /// With <c>featured=true</c> only the featured projects are listed, at most five: the start page
  /// uses that. Texts fall back to German field by field; <c>fallbackFields</c> names the fields that did.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="featured">Only the projects featured on the start page.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The cards; an empty list when there are none.</response>
  /// <response code="400">The locale is not supported.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<PublicProjectCard>>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  public Task<IReadOnlyList<PublicProjectCard>> List(
    [FromQuery] string locale = "de",
    [FromQuery] bool featured = false,
    CancellationToken cancellationToken = default)
    => projectService.ListPublicAsync(Locale.Parse(locale), featured, cancellationToken);

  /// <summary>Reads the case study of a visible project.</summary>
  /// <remarks>
  /// The case study comes as HTML rendered from the restricted Markdown subset (headings,
  /// paragraphs, lists, code, links, images from uploaded documents); all text is escaped, so it is
  /// safe to bind as inner HTML. Hidden projects, unknown slugs and malformed slugs all answer 404.
  /// </remarks>
  /// <param name="slug">The project's URL segment.</param>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The case study.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No visible project has this slug.</response>
  [HttpGet("{slug}")]
  [ProducesResponseType<PublicCaseStudy>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<PublicCaseStudy> Get(
    string slug,
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => projectService.GetCaseStudyAsync(slug, Locale.Parse(locale), cancellationToken);
}
