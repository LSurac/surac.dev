using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Legal.Application;
using SuracDev.Api.Domains.Legal.Contracts;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Legal.Controllers;

/// <summary>
/// The legal pages in the admin area. There are exactly two (Impressum and privacy page), so pages
/// are addressed by key and only read and replaced, never created or deleted.
/// </summary>
[ApiController]
[Route("api/admin/legal-pages")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminLegalPagesController(LegalPageService legalPageService) : ControllerBase
{
  /// <summary>Lists both legal pages with both locales, Impressum first.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The pages.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<IReadOnlyList<AdminLegalPage>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminLegalPage>> List(CancellationToken cancellationToken)
    => legalPageService.ListAdminAsync(cancellationToken);

  /// <summary>Reads one legal page for the editor.</summary>
  /// <param name="key"><c>Imprint</c> or <c>Privacy</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The page with both locales and their "last updated" stamps.</response>
  /// <response code="400">The key is not supported.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">The page does not exist.</response>
  [HttpGet("{key}")]
  [ProducesResponseType<AdminLegalPage>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<AdminLegalPage> Get(LegalPageKey key, CancellationToken cancellationToken)
    => legalPageService.GetAdminAsync(key, cancellationToken);

  /// <summary>Replaces the texts of a legal page.</summary>
  /// <remarks>
  /// German title and body are required; <c>en: null</c> removes the English texts, which then
  /// fall back to German. Only a locale whose text actually changed gets a new "last updated"
  /// stamp, so saving unchanged texts never moves the date the privacy page shows.
  /// </remarks>
  /// <param name="key"><c>Imprint</c> or <c>Privacy</c>.</param>
  /// <param name="form">Both locales.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">A field breaks a rule (<c>de.title</c>, <c>de.bodyMarkdown</c>) or the key is not
  /// supported.</response>
  /// <response code="401">Not logged in.</response>
  [HttpPut("{key}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Update(LegalPageKey key, LegalPageForm form, CancellationToken cancellationToken)
  {
    await legalPageService.SaveAsync(key, form, cancellationToken);
    return NoContent();
  }
}
