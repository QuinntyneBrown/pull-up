using MediatR;
using PullUp.Application.Common.Auditing;

namespace PullUp.Application.Features.Events.CreateEvent;

[AuditedAction("EVENT_CREATED")]
public sealed record CreateEventCommand(
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Location,
    string Description,
    bool AllowPlusOne,
    bool ShowGuestList,
    IReadOnlyList<string> InviteeEmails) : IRequest<CreateEventResponse>;
