using MediatR;
using PullUp.Application.Common.Auditing;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.RemoveInvitee;

[AuditedAction("EVENT_INVITEE_REMOVED")]
public sealed record RemoveInviteeCommand(Guid EventId, Guid InvitationId) : IRequest<Unit>, IAuthorizationRequirement;
