using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.UpdateEvent;

public sealed class UpdateEventAuthorizationHandler : IAuthorizationHandler<UpdateEventCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateEventAuthorizationHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> AuthorizeAsync(UpdateEventCommand request, CancellationToken cancellationToken)
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
        // Missing event -> pass so the handler can throw NotFoundException -> 404.
        return hostId is null || hostId == userId;
    }
}
