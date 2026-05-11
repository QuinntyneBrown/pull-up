namespace PullUp.Application.Features.Users.GetNotificationPreferences;

public sealed record NotificationPreferencesResponse(
    bool NewInvitations,
    bool EventReminders,
    bool RsvpChanges);
