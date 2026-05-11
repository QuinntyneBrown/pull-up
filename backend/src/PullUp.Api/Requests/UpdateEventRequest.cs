namespace PullUp.Api.Requests;

public sealed record UpdateEventRequest(
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Location,
    string? Description,
    bool AllowPlusOne,
    bool ShowGuestList);
