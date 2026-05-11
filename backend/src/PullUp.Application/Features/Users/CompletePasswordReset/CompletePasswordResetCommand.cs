using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.CompletePasswordReset;

[AuditedAction("PASSWORD_RESET_COMPLETED")]
public sealed record CompletePasswordResetCommand(string Token, string NewPassword) : IRequest<Unit>;
