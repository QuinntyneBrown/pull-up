using MediatR;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Notifications.DispatchInvitationNotification;

// Internal command used by Create/Update/AddInvitee/Cancel event handlers to
// fan out a single notification per recipient. Gating against the recipient's
// per-user NotificationPreference happens inside the handler.
public sealed record DispatchInvitationNotificationCommand(
    Guid RecipientUserId,
    Guid EventId,
    NotificationKind Kind) : IRequest<Unit>;
