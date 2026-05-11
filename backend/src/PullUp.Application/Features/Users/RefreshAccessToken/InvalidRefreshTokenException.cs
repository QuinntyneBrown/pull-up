namespace PullUp.Application.Features.Users.RefreshAccessToken;

// Single exception type for any refresh-token failure mode (unknown hash,
// revoked, expired). The API surface uses a generic 401 body so a stolen token
// cannot be probed for "still valid / already revoked".
public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Invalid or expired refresh token.")
    {
    }
}
