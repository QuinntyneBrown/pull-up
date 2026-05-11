namespace PullUp.Domain.Audit;

public sealed class AuditLogEntry
{
    public Guid Id { get; private set; }
    public string Event { get; private set; } = null!;
    public Guid? ActorUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string Outcome { get; private set; } = null!;
    public string? MetadataJson { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Record(
        string @event,
        Guid? actorUserId,
        DateTimeOffset occurredAt,
        Guid correlationId,
        string outcome,
        string? metadataJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(@event);
        ArgumentException.ThrowIfNullOrEmpty(outcome);

        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Event = @event,
            ActorUserId = actorUserId,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            Outcome = outcome,
            MetadataJson = metadataJson,
        };
    }
}
