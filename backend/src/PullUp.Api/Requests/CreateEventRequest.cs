namespace PullUp.Api.Requests;

public sealed record CreateEventRequest(
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Location,
    string? Description,
    bool AllowPlusOne,
    bool ShowGuestList,
    IReadOnlyList<string>? InviteeEmails);
