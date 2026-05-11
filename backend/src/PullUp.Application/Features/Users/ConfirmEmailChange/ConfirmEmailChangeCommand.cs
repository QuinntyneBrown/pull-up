using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.ConfirmEmailChange;

[AuditedAction("USER_EMAIL_CHANGE_CONFIRMED")]
public sealed record ConfirmEmailChangeCommand(string Token) : IRequest<Unit>;
