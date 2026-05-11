namespace PullUp.Application.Features.Users.SignInUser;

public sealed record SignInUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string DisplayName,
    string Role,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
