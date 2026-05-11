namespace PullUp.Application.Abstractions;

// Deferred-integration boundary for in-app notifications. The MVP wiring is
// LoggingNotificationSender (no-op logger); production swaps in real delivery.
public interface INotificationSender
{
    Task SendAsync(
        Guid recipientUserId,
        NotificationKind kind,
        Guid relatedEventId,
        CancellationToken cancellationToken);
}
