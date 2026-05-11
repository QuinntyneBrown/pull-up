using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.RemoveInvitee;

public sealed class RemoveInviteeAuthorizationHandler : IAuthorizationHandler<RemoveInviteeCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public RemoveInviteeAuthorizationHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> AuthorizeAsync(RemoveInviteeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return false;

        var hostId = await _db.Events
            .Where(e => e.Id == request.EventId)
            .Select(e => (Guid?)e.HostId)
            .SingleOrDefaultAsync(cancellationToken);
        return hostId is null || hostId == userId;
    }
}
