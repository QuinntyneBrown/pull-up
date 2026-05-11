namespace PullUp.Application.Features.Users.CompletePasswordReset;

// Single exception for any token failure (unknown / expired / already used) so
// the API surface cannot be probed for token validity (L2-009).
public sealed class InvalidPasswordResetTokenException : Exception
{
    public InvalidPasswordResetTokenException() : base("The password reset link is invalid or has expired.")
    {
    }
}
