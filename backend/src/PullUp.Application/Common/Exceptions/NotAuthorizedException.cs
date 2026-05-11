namespace PullUp.Application.Common.Exceptions;

public sealed class NotAuthorizedException : Exception
{
    public NotAuthorizedException()
        : base("The current user is not authorized to perform this action.")
    {
    }

    public NotAuthorizedException(string message) : base(message)
    {
    }
}
