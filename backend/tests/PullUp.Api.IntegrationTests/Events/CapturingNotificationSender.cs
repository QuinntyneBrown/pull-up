using System.Collections.Concurrent;
using PullUp.Application.Abstractions;

namespace PullUp.Api.IntegrationTests.Events;

public sealed record CapturedNotification(Guid RecipientUserId, NotificationKind Kind, Guid EventId);

public sealed class CapturingNotificationSender : INotificationSender
{
    public ConcurrentBag<CapturedNotification> Sent { get; } = new();

    public Task SendAsync(
        Guid recipientUserId,
        NotificationKind kind,
        Guid relatedEventId,
        CancellationToken cancellationToken)
    {
        Sent.Add(new CapturedNotification(recipientUserId, kind, relatedEventId));
        return Task.CompletedTask;
    }
}
