namespace PullUp.Application.Features.Events.GetEvent;

public sealed record GetEventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Location,
    string Description,
    string Status,
    bool AllowPlusOne,
    bool ShowGuestList,
    HostSummary Host,
    bool IsHost,
    string? MyRsvpStatus,
    int GoingCount,
    int MaybeCount,
    int CantGoCount,
    IReadOnlyList<GuestSummary>? Guests);
