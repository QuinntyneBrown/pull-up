using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PullUp.Api.Requests;
using PullUp.Application.Features.Events.AddInvitee;
using PullUp.Application.Features.Events.CancelEvent;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Events.ListMyEvents;
using PullUp.Application.Features.Events.RemoveInvitee;
using PullUp.Application.Features.Events.SetRsvp;
using PullUp.Application.Features.Events.UpdateEvent;

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

    [HttpGet]
    [ProducesResponseType(typeof(ListMyEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ListMyEventsQuery(scope), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetEventQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelEventCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEventCommand(
            id,
            request.Title,
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.Location,
            request.Description ?? string.Empty,
            request.AllowPlusOne,
            request.ShowGuestList);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invitees")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddInvitee(
        Guid id,
        [FromBody] AddInviteeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddInviteeCommand(id, request.Email), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/invitees/{invitationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveInvitee(
        Guid id,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveInviteeCommand(id, invitationId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/rsvp")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetRsvp(
        Guid id,
        [FromBody] SetRsvpRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetRsvpCommand(id, request.Status, request.Note), cancellationToken);
        return NoContent();
    }
}
