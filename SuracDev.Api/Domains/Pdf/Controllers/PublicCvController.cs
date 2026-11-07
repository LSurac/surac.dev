using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Pdf.Application;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Pdf.Controllers;

/// <summary>The CV as PDF for the open site.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/cv.pdf")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
public sealed class PublicCvController(CvService cvService) : ControllerBase
{
  /// <summary>Downloads the public CV as PDF.</summary>
  /// <remarks>
  /// The <c>Public</c> variant: everything the open site shows, never phone number, street address
  /// or hidden entries. Rendered on demand from the current content, one column with real text and
  /// standard headings so applicant tracking systems can parse it. The file is named
  /// <c>Lebenslauf_&lt;Name&gt;.pdf</c> in German and <c>CV_&lt;Name&gt;.pdf</c> in English.
  /// </remarks>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The PDF as attachment.</response>
  /// <response code="400">The locale is not supported.</response>
  /// <response code="404">No profile exists yet.</response>
  [HttpGet]
  [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Get([FromQuery] string locale = "de", CancellationToken cancellationToken = default)
  {
    var cv = await cvService.RenderPublicAsync(Locale.Parse(locale), cancellationToken);
    return File(cv.Content, "application/pdf", cv.FileName);
  }
}
