using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;
using PullUp.Application.Features.Notifications.DispatchInvitationNotification;

namespace PullUp.Application.Features.Events.UpdateEvent;

public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;

    public UpdateEventCommandHandler(IAppDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _db.Events.SingleOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        // Snapshot the fields that drive a notification BEFORE we mutate, so we
        // can detect a change after UpdateDetails runs.
        var prevStartsAt = @event.StartsAtUtc;
        var prevEndsAt = @event.EndsAtUtc;
        var prevLocation = @event.Location;

        var now = DateTimeOffset.UtcNow;
        @event.UpdateDetails(
            title: request.Title,
            startsAtUtc: request.StartsAtUtc,
            endsAtUtc: request.EndsAtUtc,
            location: request.Location,
            description: request.Description ?? string.Empty,
            allowPlusOne: request.AllowPlusOne,
            showGuestList: request.ShowGuestList,
            now: now);

        var dispatchNeeded =
            prevStartsAt != @event.StartsAtUtc ||
            prevEndsAt != @event.EndsAtUtc ||
            !string.Equals(prevLocation, @event.Location, StringComparison.Ordinal);

        await _db.SaveChangesAsync(cancellationToken);

        if (dispatchNeeded)
        {
            var inviteeUserIds = await _db.Invitations
                .Where(i => i.EventId == @event.Id && i.RemovedAt == null && i.UserId != null)
                .Select(i => i.UserId!.Value)
                .ToListAsync(cancellationToken);

            foreach (var recipient in inviteeUserIds)
            {
                await _mediator.Send(
                    new DispatchInvitationNotificationCommand(recipient, @event.Id, NotificationKind.EventUpdated),
                    cancellationToken);
            }
        }

        return Unit.Value;
    }
}
