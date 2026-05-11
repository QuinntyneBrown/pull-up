using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.ListMyEvents;

public sealed class ListMyEventsQueryHandler : IRequestHandler<ListMyEventsQuery, ListMyEventsResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ListMyEventsQueryHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ListMyEventsResponse> Handle(ListMyEventsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var now = DateTimeOffset.UtcNow;

        // All events where the user is host OR has an active invitation. Pull the
        // candidates from SQL on identity criteria only; apply the date-based
        // scope filter on the client side. DateTimeOffset comparisons translate
        // inconsistently across SQL Server and SQLite, and the candidate set is
        // bounded by the user's events.
        var baseQuery = _db.Events.AsNoTracking().Where(e =>
            e.HostId == userId ||
            _db.Invitations.Any(i => i.EventId == e.Id && i.UserId == userId && i.RemovedAt == null));

        var events = await baseQuery.ToListAsync(cancellationToken);

        events = (request.Scope?.ToLowerInvariant()) switch
        {
            "hosting" => events.Where(e => e.HostId == userId).ToList(),
            "invited" => events.Where(e => e.HostId != userId).ToList(),
            "past" => events.Where(e => e.StartsAtUtc < now).ToList(),
            _ => events,
        };

        var eventIds = events.Select(e => e.Id).ToList();
        var rsvps = await _db.Rsvps.AsNoTracking()
            .Where(r => r.UserId == userId && eventIds.Contains(r.EventId))
            .ToListAsync(cancellationToken);
        var rsvpByEvent = rsvps.ToDictionary(r => r.EventId, r => r.Status);

        EventSummary ToSummary(Event e) => new(
            e.Id,
            e.Title,
            e.StartsAtUtc,
            e.Location,
            e.Status.ToString(),
            e.HostId == userId,
            rsvpByEvent.TryGetValue(e.Id, out var s) ? s.ToString() : null);

        var thisWeekEnd = now.AddDays(7);
        var endOfMonth = new DateTimeOffset(
            now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month),
            23, 59, 59, TimeSpan.Zero);
        var endOfNextMonth = endOfMonth.AddMonths(1);

        var thisWeek = events
            .Where(e => e.StartsAtUtc >= now && e.StartsAtUtc < thisWeekEnd)
            .OrderBy(e => e.StartsAtUtc).Select(ToSummary).ToList();
        var laterThisMonth = events
            .Where(e => e.StartsAtUtc >= thisWeekEnd && e.StartsAtUtc <= endOfMonth)
            .OrderBy(e => e.StartsAtUtc).Select(ToSummary).ToList();
        var nextMonth = events
            .Where(e => e.StartsAtUtc > endOfMonth && e.StartsAtUtc <= endOfNextMonth)
            .OrderBy(e => e.StartsAtUtc).Select(ToSummary).ToList();
        var past = events
            .Where(e => e.StartsAtUtc < now)
            .OrderByDescending(e => e.StartsAtUtc).Select(ToSummary).ToList();

        return new ListMyEventsResponse(thisWeek, laterThisMonth, nextMonth, past);
    }
}
