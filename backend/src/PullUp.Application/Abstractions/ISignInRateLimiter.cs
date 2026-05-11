namespace PullUp.Application.Abstractions;

// Tracks repeated failed sign-in attempts per email. The sign-in handler calls
// EnsureNotLocked before checking credentials, RegisterFailedAttempt on a 401,
// and ResetAttempts on a successful sign-in.
public interface ISignInRateLimiter
{
    void EnsureNotLocked(string email);

    void RegisterFailedAttempt(string email);

    void ResetAttempts(string email);
}
