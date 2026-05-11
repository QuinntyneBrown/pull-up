using MediatR;
using PullUp.Application.Common.Auditing;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.CancelEvent;

[AuditedAction("EVENT_CANCELLED")]
public sealed record CancelEventCommand(Guid EventId) : IRequest<Unit>, IAuthorizationRequirement;
