using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.UpdateNotificationPreferences;

[AuditedAction("USER_NOTIFICATION_PREFS_UPDATED")]
public sealed record UpdateNotificationPreferencesCommand(
    bool NewInvitations,
    bool EventReminders,
    bool RsvpChanges) : IRequest<Unit>;
