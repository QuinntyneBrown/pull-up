namespace PullUp.Api.Requests;

public sealed record RegisterUserRequest(
    string FullName,
    string Email,
    string Password);
