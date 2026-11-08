using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Applications.Controllers;

/// <summary>
/// An application link as its recipient uses it. The token in the path is the only credential;
/// unknown, expired and revoked tokens answer the same 404. Rate limited per client address.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/links/{token}")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[EnableRateLimiting(RateLimitPolicies.Link)]
public sealed class PublicApplicationLinksController(LinkAccessService linkAccessService) : ControllerBase
{
  /// <summary>Opens an application link.</summary>
  /// <remarks>
  /// Returns what the link changes on the start page: the greeting, the section order of its
  /// audience profile and the downloads it unlocks. The content itself comes from the public
  /// endpoints and is the same for everyone. Records an <c>Opened</c> event with time and locale,
  /// nothing about the visitor.
  /// </remarks>
  /// <param name="token">The link's token from <c>/b/{token}</c>.</param>
  /// <param name="locale">Language of the greeting and document labels, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The link's greeting, sections and downloads.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No active link has this token.</response>
  /// <response code="429">Too many requests from this address.</response>
  [HttpGet]
  [Produces("application/json")]
  [ProducesResponseType<PublicApplicationLink>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
  public Task<PublicApplicationLink> Get(
    string token,
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => linkAccessService.OpenAsync(token, Locale.Parse(locale), cancellationToken);

  /// <summary>Downloads the CV this link unlocks.</summary>
  /// <remarks>
  /// The full variant with phone and street address when the link unlocks it, the public variant
  /// otherwise; same file names as the open site. Records a <c>PdfDownloaded</c> event.
  /// </remarks>
  /// <param name="token">The link's token.</param>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The PDF as attachment.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No active link has this token.</response>
  /// <response code="429">Too many requests from this address.</response>
  [HttpGet("cv.pdf")]
  [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
  public async Task<IActionResult> GetCv(
    string token,
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
  {
    var cv = await linkAccessService.RenderCvAsync(token, Locale.Parse(locale), cancellationToken);
    return File(cv.Content, "application/pdf", cv.FileName);
  }

  /// <summary>Downloads a document attached to this link.</summary>
  /// <remarks>
  /// Only documents the admin attached to this link are delivered, always as attachment with their
  /// original file name; any other id answers 404. Records a <c>DocumentDownloaded</c> event.
  /// </remarks>
  /// <param name="token">The link's token.</param>
  /// <param name="id">The document's identifier from the link's document list.</param>
  /// <param name="locale">The visitor's language, recorded with the event; <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The file as attachment.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No active link has this token, or the link has no such document.</response>
  /// <response code="429">Too many requests from this address.</response>
  [HttpGet("documents/{id:long}")]
  [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
  public async Task<IActionResult> GetDocument(
    string token,
    long id,
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
  {
    var document = await linkAccessService.OpenDocumentAsync(token, id, Locale.Parse(locale), cancellationToken);
    return File(document.Content, document.ContentType, document.FileName);
  }
}
