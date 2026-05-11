using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Notifications;

namespace PullUp.Application.Features.Users.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public GetNotificationPreferencesQueryHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<NotificationPreferencesResponse> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var prefs = await _db.NotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? NotificationPreference.DefaultFor(userId);

        return new NotificationPreferencesResponse(
            prefs.NewInvitations,
            prefs.EventReminders,
            prefs.RsvpChanges);
    }
}
