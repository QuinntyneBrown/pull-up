namespace PullUp.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string DisplayName,
    string Role,
    DateTimeOffset CreatedAt);
