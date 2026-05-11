using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.SignOut;

[AuditedAction("USER_SIGNED_OUT")]
public sealed record SignOutCommand(string RefreshToken) : IRequest<Unit>;
