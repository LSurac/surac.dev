using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Legal.Application;
using SuracDev.Api.Domains.Legal.Contracts;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Legal.Controllers;

/// <summary>Impressum and privacy page of the open site.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/legal")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
[Produces("application/json")]
public sealed class PublicLegalController(LegalPageService legalPageService) : ControllerBase
{
  /// <summary>Reads a legal page.</summary>
  /// <remarks>
  /// <c>key</c> is <c>Imprint</c> or <c>Privacy</c> (case-insensitive). The body comes as HTML
  /// rendered from the restricted Markdown subset and is safe to bind as inner HTML.
  /// <c>updatedAt</c> is the "last updated" date of the locale shown. The Impressum additionally
  /// carries <c>contact</c> with name, postal address and e-mail from the profile, as § 5 DDG
  /// requires; no other public response contains the address.
  /// </remarks>
  /// <param name="key">Which page.</param>
  /// <param name="locale">Content language, <c>de</c> (default) or <c>en</c>.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The page in the requested locale.</response>
  /// <response code="400">The key or the locale is not supported.</response>
  /// <response code="404">The page does not exist.</response>
  [HttpGet("{key}")]
  [ProducesResponseType<PublicLegalPage>(StatusCodes.Status200OK)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public Task<PublicLegalPage> Get(
    LegalPageKey key,
    [FromQuery] string locale = "de",
    CancellationToken cancellationToken = default)
    => legalPageService.GetPublicAsync(key, Locale.Parse(locale), cancellationToken);
}
