using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.SetRsvp;

public sealed class SetRsvpAuthorizationHandler : IAuthorizationHandler<SetRsvpCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public SetRsvpAuthorizationHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> AuthorizeAsync(SetRsvpCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return false;

        // Pass auth when the event doesn't exist so the handler can throw NotFoundException -> 404.
        var hostId = await _db.Events
            .Where(e => e.Id == request.EventId)
            .Select(e => (Guid?)e.HostId)
            .SingleOrDefaultAsync(cancellationToken);
        if (hostId is null) return true;
        if (hostId == userId) return true;

        // Otherwise: invitee must have an active invitation.
        return await _db.Invitations
            .AnyAsync(
                i => i.EventId == request.EventId && i.UserId == userId && i.RemovedAt == null,
                cancellationToken);
    }
}
