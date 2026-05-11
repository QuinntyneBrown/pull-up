namespace PullUp.Application.Common.Exceptions;

public sealed class SignInRateLimitExceededException : Exception
{
    public SignInRateLimitExceededException(int retryAfterSeconds)
        : base("Too many sign-in attempts. Try again later.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int RetryAfterSeconds { get; }
}
