namespace PullUp.Application.Features.Users.SignInUser;

// Single exception type for "wrong email or wrong password". The handler must
// throw this in BOTH cases (unknown email + wrong password) so the API response
// is identical and does not reveal whether the email exists (L2-005).
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.")
    {
    }
}
