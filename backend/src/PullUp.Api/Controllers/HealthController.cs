using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Infrastructure.Persistence;

namespace PullUp.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live() => Ok(new { status = "healthy" });

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var dbOk = await db.Database.CanConnectAsync(cancellationToken);
        if (!dbOk)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not-ready", db = "unreachable" });
        }
        return Ok(new { status = "ready" });
    }
}
