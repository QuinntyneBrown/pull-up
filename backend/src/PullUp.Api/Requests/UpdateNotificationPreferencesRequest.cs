namespace PullUp.Api.Requests;

public sealed record UpdateNotificationPreferencesRequest(
    bool NewInvitations,
    bool EventReminders,
    bool RsvpChanges);
