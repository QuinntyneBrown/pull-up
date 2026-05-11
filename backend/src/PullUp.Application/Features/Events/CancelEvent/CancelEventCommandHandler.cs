using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;
using PullUp.Application.Features.Notifications.DispatchInvitationNotification;

namespace PullUp.Application.Features.Events.CancelEvent;

public sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;

    public CancelEventCommandHandler(IAppDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _db.Events.SingleOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var now = DateTimeOffset.UtcNow;
        @event.Cancel(now);

        var inviteeUserIds = await _db.Invitations
            .Where(i => i.EventId == @event.Id && i.RemovedAt == null && i.UserId != null)
            .Select(i => i.UserId!.Value)
            .ToListAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var recipient in inviteeUserIds)
        {
            await _mediator.Send(
                new DispatchInvitationNotificationCommand(recipient, @event.Id, NotificationKind.EventCancelled),
                cancellationToken);
        }

        return Unit.Value;
    }
}
