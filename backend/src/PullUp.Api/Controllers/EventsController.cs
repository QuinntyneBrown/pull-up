using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Api.Requests;
using PullUp.Application.Features.Events.CreateEvent;

namespace PullUp.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventsController : ControllerBase
{
    private readonly ISender _mediator;

    public EventsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(
            request.Title,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.Location,
            request.Description ?? string.Empty,
            request.AllowPlusOne,
            request.ShowGuestList,
            request.InviteeEmails ?? Array.Empty<string>());

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
    }
}
