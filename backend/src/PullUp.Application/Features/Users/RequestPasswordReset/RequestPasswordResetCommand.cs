using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.RequestPasswordReset;

[AuditedAction("PASSWORD_RESET_REQUESTED")]
public sealed record RequestPasswordResetCommand(string Email) : IRequest<Unit>;
