using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.RequestEmailChange;

[AuditedAction("USER_EMAIL_CHANGE_REQUESTED")]
public sealed record RequestEmailChangeCommand(string NewEmail, string CurrentPassword) : IRequest<Unit>;
