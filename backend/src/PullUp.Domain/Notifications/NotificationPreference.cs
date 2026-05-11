namespace PullUp.Domain.Notifications;

public sealed class NotificationPreference
{
    public Guid UserId { get; private set; }
    public bool NewInvitations { get; private set; }
    public bool EventReminders { get; private set; }
    public bool RsvpChanges { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference DefaultFor(Guid userId)
        => new()
        {
            UserId = userId,
            NewInvitations = true,
            EventReminders = true,
            RsvpChanges = true,
        };

    public void Update(bool newInvitations, bool eventReminders, bool rsvpChanges)
    {
        NewInvitations = newInvitations;
        EventReminders = eventReminders;
        RsvpChanges = rsvpChanges;
    }
}
