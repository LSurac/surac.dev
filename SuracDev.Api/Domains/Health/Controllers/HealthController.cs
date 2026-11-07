using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Data;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Health.Controllers;

/// <summary>
/// Liveness and readiness in one: the Docker healthcheck and the uptime monitor call it, and it
/// only answers OK when the database is reachable.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/health")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
public sealed class HealthController(SuracDevDbContext dbContext) : ControllerBase
{
  public sealed record HealthResponse(string Status);

  [HttpGet]
  [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
  [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
  public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
  {
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
      ? Ok(new HealthResponse("ok"))
      : StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse("db-unreachable"));
  }
}
