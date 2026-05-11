using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Api.Requests;
using PullUp.Application.Features.Users.ConfirmEmailChange;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.GetNotificationPreferences;
using PullUp.Application.Features.Users.RegisterUser;
using PullUp.Application.Features.Users.RequestEmailChange;
using PullUp.Application.Features.Users.UpdateNotificationPreferences;
using PullUp.Application.Features.Users.UpdateProfile;

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

    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateProfileCommand(request.FullName, request.DisplayName), cancellationToken);
        return NoContent();
    }

    [HttpPost("me/email-change")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestEmailChange(
        [FromBody] RequestEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RequestEmailChangeCommand(request.NewEmail, request.CurrentPassword),
            cancellationToken);
        return Accepted();
    }

    [HttpPost("me/email-change/confirm")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmEmailChangeCommand(request.Token), cancellationToken);
        return NoContent();
    }

    [HttpGet("me/notification-preferences")]
    [Authorize]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetNotificationPreferencesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPut("me/notification-preferences")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateNotificationPreferencesCommand(request.NewInvitations, request.EventReminders, request.RsvpChanges),
            cancellationToken);
        return NoContent();
    }
}
