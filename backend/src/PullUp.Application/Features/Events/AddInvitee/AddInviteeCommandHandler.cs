using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;
using PullUp.Application.Features.Notifications.DispatchInvitationNotification;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.AddInvitee;

public sealed class AddInviteeCommandHandler : IRequestHandler<AddInviteeCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;

    public AddInviteeCommandHandler(IAppDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(AddInviteeCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException("Event", request.EventId);
        }

        var normalized = request.Email.Trim().ToLowerInvariant();

        // Idempotent: if an active invitation already exists for this email + event,
        // skip the insert (and the dispatch) but return success.
        var existing = await _db.Invitations.AsNoTracking()
            .Where(i => i.EventId == request.EventId && i.InvitedEmail == normalized && i.RemovedAt == null)
            .Select(i => new { i.Id, i.UserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return Unit.Value;
        }

        var matchedUserId = await _db.Users.AsNoTracking()
            .Where(u => u.Email == normalized)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var invitation = Invitation.Create(request.EventId, matchedUserId, normalized, now);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(cancellationToken);

        if (matchedUserId is Guid uid)
        {
            await _mediator.Send(
                new DispatchInvitationNotificationCommand(uid, request.EventId, NotificationKind.EventInvited),
                cancellationToken);
        }

        return Unit.Value;
    }
}
