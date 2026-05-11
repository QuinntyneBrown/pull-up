namespace PullUp.Application.Features.Events.ListMyEvents;

public sealed record EventSummary(
    Guid Id,
    string Title,
    DateTimeOffset StartsAtUtc,
    string Location,
    string Status,
    bool IsHost,
    string? MyRsvpStatus);
