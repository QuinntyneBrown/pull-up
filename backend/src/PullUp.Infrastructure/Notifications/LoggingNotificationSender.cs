using Microsoft.Extensions.Logging;
using PullUp.Application.Abstractions;

namespace PullUp.Infrastructure.Notifications;

// MVP no-op per BP1 plan §9 "deferred integrations". Logs only metadata —
// never raw payload — so future BI1 slices can swap in a real implementation
// behind the same interface without touching callers.
public sealed class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        Guid recipientUserId,
        NotificationKind kind,
        Guid relatedEventId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Notification {Kind} queued for user {RecipientUserId} (event {EventId})",
            kind,
            recipientUserId,
            relatedEventId);
        return Task.CompletedTask;
    }
}
