using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Applications.Application;
using SuracDev.Api.Domains.Applications.Contracts;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Applications.Controllers;

/// <summary>The statistics of applications and their links in the admin area.</summary>
[ApiController]
[Route("api/admin/application-statistics")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
[Produces("application/json")]
public sealed class AdminApplicationStatisticsController(
  ApplicationStatisticsService statisticsService) : ControllerBase
{
  /// <summary>Reads the statistics page.</summary>
  /// <remarks>
  /// The status pipeline with a count per status, the response rate (every status except
  /// <c>Draft</c> counts as sent; reply, interview, offer and rejection count as a response) and per
  /// application its links' openings, downloads and the time from the first link to the first
  /// opening. Built from link events, which carry no visitor data.
  /// </remarks>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The statistics.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [ProducesResponseType<ApplicationStatistics>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<ApplicationStatistics> Get(CancellationToken cancellationToken)
    => statisticsService.GetAsync(cancellationToken);
}
