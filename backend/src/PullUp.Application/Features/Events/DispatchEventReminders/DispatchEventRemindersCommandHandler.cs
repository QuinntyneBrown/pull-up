using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Notifications.DispatchInvitationNotification;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.DispatchEventReminders;

public sealed class DispatchEventRemindersCommandHandler : IRequestHandler<DispatchEventRemindersCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;

    public DispatchEventRemindersCommandHandler(IAppDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(DispatchEventRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        // Candidate Rsvps: status Going or Maybe, event Scheduled (not Cancelled),
        // event starts in [now+23h, now+25h], and reminder not yet sent. Filter
        // dates client-side to avoid SQLite DateTimeOffset translation quirks
        // (same pattern as ListMyEvents).
        var candidateRsvps = await (
            from r in _db.Rsvps
            join e in _db.Events on r.EventId equals e.Id
            where r.ReminderSentAt == null
                && (r.Status == RsvpStatus.Going || r.Status == RsvpStatus.Maybe)
                && e.Status == EventStatus.Scheduled
            select new { Rsvp = r, EventStart = e.StartsAtUtc, EventId = e.Id }
        ).ToListAsync(cancellationToken);

        var ready = candidateRsvps
            .Where(c => c.EventStart >= windowStart && c.EventStart <= windowEnd)
            .ToList();

        foreach (var c in ready)
        {
            c.Rsvp.MarkReminderSent(now);
        }
        await _db.SaveChangesAsync(cancellationToken);

        // Dispatch after save so a notification-side failure can't silently
        // "lose" the reminder — the row is already marked, and the dispatcher
        // logs / no-ops on failures.
        foreach (var c in ready)
        {
            await _mediator.Send(
                new DispatchInvitationNotificationCommand(c.Rsvp.UserId, c.EventId, NotificationKind.EventReminder),
                cancellationToken);
        }

        return Unit.Value;
    }
}
