using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.RegisterUser;

[AuditedAction("USER_REGISTERED")]
public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password) : IRequest<RegisterUserResponse>;
