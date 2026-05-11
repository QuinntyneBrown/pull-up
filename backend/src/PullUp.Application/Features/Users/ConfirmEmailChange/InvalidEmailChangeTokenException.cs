namespace PullUp.Application.Features.Users.ConfirmEmailChange;

public sealed class InvalidEmailChangeTokenException : Exception
{
    public InvalidEmailChangeTokenException()
        : base("The email-change verification link is invalid or has expired.")
    {
    }
}
