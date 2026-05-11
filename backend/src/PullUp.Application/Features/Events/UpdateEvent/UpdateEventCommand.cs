using MediatR;
using PullUp.Application.Common.Auditing;
using PullUp.Application.Common.Authorization;

namespace PullUp.Application.Features.Events.UpdateEvent;

[AuditedAction("EVENT_UPDATED")]
public sealed record UpdateEventCommand(
    Guid EventId,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Location,
    string Description,
    bool AllowPlusOne,
    bool ShowGuestList) : IRequest<Unit>, IAuthorizationRequirement;
