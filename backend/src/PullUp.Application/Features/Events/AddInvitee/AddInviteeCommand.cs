using MediatR;
using PullUp.Application.Common.Auditing;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.AddInvitee;

[AuditedAction("EVENT_INVITEE_ADDED")]
public sealed record AddInviteeCommand(Guid EventId, string Email) : IRequest<Unit>, IAuthorizationRequirement;
