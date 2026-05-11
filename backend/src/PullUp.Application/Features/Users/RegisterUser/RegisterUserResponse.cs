namespace PullUp.Application.Features.Users.RegisterUser;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string DisplayName,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt);
