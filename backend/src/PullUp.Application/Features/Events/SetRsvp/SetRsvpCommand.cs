using MediatR;
using PullUp.Application.Common.Auditing;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.SetRsvp;

[AuditedAction("EVENT_RSVP_SET")]
public sealed record SetRsvpCommand(Guid EventId, string Status, string? Note)
    : IRequest<Unit>, IAuthorizationRequirement;
