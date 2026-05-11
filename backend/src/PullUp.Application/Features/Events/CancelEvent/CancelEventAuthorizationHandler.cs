using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.CancelEvent;

public sealed class CancelEventAuthorizationHandler : IAuthorizationHandler<CancelEventCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public CancelEventAuthorizationHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> AuthorizeAsync(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return false;
        }

        var hostId = await _db.Events
            .Where(e => e.Id == request.EventId)
            .Select(e => (Guid?)e.HostId)
            .SingleOrDefaultAsync(cancellationToken);

        // Event doesn't exist -> pass auth so the handler can throw NotFoundException
        // (HTTP 404). Otherwise host-only.
        return hostId is null || hostId == userId;
    }
}
