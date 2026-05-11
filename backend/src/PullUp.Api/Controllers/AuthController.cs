using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Api.Requests;
using PullUp.Application.Features.Users.RefreshAccessToken;
using PullUp.Application.Features.Users.SignInUser;

namespace PullUp.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(SignInUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(
        [FromBody] SignInUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SignInUserCommand(request.Email, request.Password);
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshAccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshAccessTokenCommand(request.RefreshToken);
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}
