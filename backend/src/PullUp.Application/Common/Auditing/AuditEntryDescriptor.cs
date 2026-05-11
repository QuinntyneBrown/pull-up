namespace PullUp.Application.Common.Auditing;

public sealed record AuditEntryDescriptor(
    string Event,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    string Outcome,
    string? MetadataJson);
