namespace PullUp.Application.Features.Events.GetEvent;

public sealed record GuestSummary(
    Guid? UserId,
    string Email,
    string? FullName,
    string? DisplayName,
    string? RsvpStatus,
    string? Note);
