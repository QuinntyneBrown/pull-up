namespace PullUp.Domain.Events;

public sealed class Invitation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }
    public string InvitedEmail { get; private set; } = null!;
    public DateTimeOffset InvitedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }

    private Invitation() { }

    public static Invitation Create(Guid eventId, Guid? userId, string invitedEmail, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(invitedEmail);
        return new Invitation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            InvitedEmail = invitedEmail.Trim().ToLowerInvariant(),
            InvitedAt = now,
        };
    }

    public void Remove(DateTimeOffset now)
    {
        if (RemovedAt is not null) return;
        RemovedAt = now;
    }
}
