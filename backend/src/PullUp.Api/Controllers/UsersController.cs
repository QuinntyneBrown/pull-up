using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Api.Requests;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.RegisterUser;

namespace PullUp.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _mediator;

    public UsersController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.FullName, request.Email, request.Password);
        var response = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetCurrent), new { }, response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(GetCurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(response);
    }
}
