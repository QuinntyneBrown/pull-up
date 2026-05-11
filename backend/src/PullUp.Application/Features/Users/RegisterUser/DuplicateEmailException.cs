namespace PullUp.Application.Features.Users.RegisterUser;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"An account with email '{email}' already exists.")
    {
        Email = email;
    }

    public string Email { get; }
}
