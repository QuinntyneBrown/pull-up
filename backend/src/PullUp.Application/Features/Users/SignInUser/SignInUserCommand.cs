using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.SignInUser;

[AuditedAction("USER_SIGNED_IN")]
public sealed record SignInUserCommand(string Email, string Password) : IRequest<SignInUserResponse>;
