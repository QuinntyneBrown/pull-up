using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Users.DeleteAccount;

[AuditedAction("ACCOUNT_DELETED")]
public sealed record DeleteAccountCommand(string CurrentPassword) : IRequest<Unit>;
