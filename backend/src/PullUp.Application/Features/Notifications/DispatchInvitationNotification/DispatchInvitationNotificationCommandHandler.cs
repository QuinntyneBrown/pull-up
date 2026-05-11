using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Notifications.DispatchInvitationNotification;

public sealed class DispatchInvitationNotificationCommandHandler
    : IRequestHandler<DispatchInvitationNotificationCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INotificationSender _sender;

    public DispatchInvitationNotificationCommandHandler(IAppDbContext db, INotificationSender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<Unit> Handle(
        DispatchInvitationNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var prefs = await _db.NotificationPreferences.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == request.RecipientUserId, cancellationToken);

        // Match each kind to its preference flag. Missing prefs row defaults to
        // all-on so users created before BT-014 still receive notifications.
        var enabled = request.Kind switch
        {
            NotificationKind.EventInvited or
            NotificationKind.EventUpdated or
            NotificationKind.EventCancelled => prefs?.NewInvitations ?? true,
            NotificationKind.EventReminder => prefs?.EventReminders ?? true,
            NotificationKind.RsvpChanged => prefs?.RsvpChanges ?? true,
            _ => true,
        };
        if (!enabled)
        {
            return Unit.Value;
        }

        await _sender.SendAsync(request.RecipientUserId, request.Kind, request.EventId, cancellationToken);
        return Unit.Value;
    }
}
