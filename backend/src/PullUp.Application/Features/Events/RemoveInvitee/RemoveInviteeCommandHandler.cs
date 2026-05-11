using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;

namespace PullUp.Application.Features.Events.RemoveInvitee;

public sealed class RemoveInviteeCommandHandler : IRequestHandler<RemoveInviteeCommand, Unit>
{
    private readonly IAppDbContext _db;

    public RemoveInviteeCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(RemoveInviteeCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _db.Invitations
            .SingleOrDefaultAsync(
                i => i.Id == request.InvitationId && i.EventId == request.EventId,
                cancellationToken)
            ?? throw new NotFoundException("Invitation", request.InvitationId);

        invitation.Remove(DateTimeOffset.UtcNow);

        if (invitation.UserId is Guid uid)
        {
            var rsvp = await _db.Rsvps.SingleOrDefaultAsync(
                r => r.EventId == request.EventId && r.UserId == uid,
                cancellationToken);
            if (rsvp is not null)
            {
                _db.Rsvps.Remove(rsvp);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
