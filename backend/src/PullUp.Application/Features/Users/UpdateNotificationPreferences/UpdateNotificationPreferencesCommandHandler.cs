using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Notifications;

namespace PullUp.Application.Features.Users.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateNotificationPreferencesCommandHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var prefs = await _db.NotificationPreferences.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (prefs is null)
        {
            prefs = NotificationPreference.DefaultFor(userId);
            _db.NotificationPreferences.Add(prefs);
        }

        prefs.Update(request.NewInvitations, request.EventReminders, request.RsvpChanges);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
