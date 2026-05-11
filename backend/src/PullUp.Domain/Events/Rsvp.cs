namespace PullUp.Domain.Events;

public sealed class Rsvp
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public RsvpStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ReminderSentAt { get; private set; }

    private Rsvp() { }

    public static Rsvp Create(Guid eventId, Guid userId, RsvpStatus status, string? note, DateTimeOffset now)
    {
        return new Rsvp
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = status,
            Note = note?.Trim(),
            UpdatedAt = now,
        };
    }

    public void Update(RsvpStatus status, string? note, DateTimeOffset now)
    {
        Status = status;
        Note = note?.Trim();
        UpdatedAt = now;
        // Status changes (e.g. CantGo -> Going) reset reminder eligibility so a
        // user who flipped to attending after the original dispatch still gets one.
        ReminderSentAt = null;
    }

    public void MarkReminderSent(DateTimeOffset now)
    {
        ReminderSentAt = now;
    }
}
