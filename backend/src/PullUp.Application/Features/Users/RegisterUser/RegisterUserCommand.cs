using MediatR;

namespace PullUp.Application.Features.Users.RegisterUser;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password) : IRequest<RegisterUserResponse>;
