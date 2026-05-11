using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;
using PullUp.Application.Features.Notifications.DispatchInvitationNotification;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.SetRsvp;

public sealed class SetRsvpCommandHandler : IRequestHandler<SetRsvpCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISender _mediator;

    public SetRsvpCommandHandler(IAppDbContext db, ICurrentUserAccessor currentUser, ISender mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(SetRsvpCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var @event = await _db.Events
            .SingleOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var now = DateTimeOffset.UtcNow;
        if (@event.StartsAtUtc <= now)
        {
            throw new EventAlreadyPassedException();
        }

        var status = Enum.Parse<RsvpStatus>(request.Status);

        var rsvp = await _db.Rsvps.SingleOrDefaultAsync(
            r => r.EventId == request.EventId && r.UserId == userId,
            cancellationToken);
        if (rsvp is null)
        {
            rsvp = Rsvp.Create(request.EventId, userId, status, request.Note, now);
            _db.Rsvps.Add(rsvp);
        }
        else
        {
            rsvp.Update(status, request.Note, now);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Notify the host (skip if the host is the one RSVPing). The dispatcher
        // gates on the host's RsvpChanges preference.
        if (@event.HostId != userId)
        {
            await _mediator.Send(
                new DispatchInvitationNotificationCommand(@event.HostId, @event.Id, NotificationKind.RsvpChanged),
                cancellationToken);
        }

        return Unit.Value;
    }
}
