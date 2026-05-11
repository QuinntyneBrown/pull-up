using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.GetEvent;

public sealed class GetEventQueryHandler : IRequestHandler<GetEventQuery, GetEventResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public GetEventQueryHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetEventResponse> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var @event = await _db.Events.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Event", request.Id);

        var isHost = @event.HostId == userId;
        var hasActiveInvitation = await _db.Invitations.AsNoTracking()
            .AnyAsync(
                i => i.EventId == @event.Id && i.UserId == userId && i.RemovedAt == null,
                cancellationToken);

        if (!isHost && !hasActiveInvitation)
        {
            throw new NotAuthorizedException();
        }

        var host = await _db.Users.AsNoTracking()
            .Where(u => u.Id == @event.HostId)
            .Select(u => new HostSummary(u.Id, u.FullName, u.DisplayName, u.Email))
            .SingleAsync(cancellationToken);

        var rsvps = await _db.Rsvps.AsNoTracking()
            .Where(r => r.EventId == @event.Id)
            .ToListAsync(cancellationToken);

        var goingCount = rsvps.Count(r => r.Status == RsvpStatus.Going);
        var maybeCount = rsvps.Count(r => r.Status == RsvpStatus.Maybe);
        var cantGoCount = rsvps.Count(r => r.Status == RsvpStatus.CantGo);
        var myRsvp = rsvps.SingleOrDefault(r => r.UserId == userId);

        IReadOnlyList<GuestSummary>? guests = null;
        if (isHost || @event.ShowGuestList)
        {
            var invitations = await _db.Invitations.AsNoTracking()
                .Where(i => i.EventId == @event.Id && i.RemovedAt == null)
                .ToListAsync(cancellationToken);

            var inviteeUserIds = invitations.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value).ToList();
            var inviteeUsersById = await _db.Users.AsNoTracking()
                .Where(u => inviteeUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            var rsvpByUser = rsvps.ToDictionary(r => r.UserId);

            var guestList = new List<GuestSummary>();
            foreach (var i in invitations)
            {
                string? fullName = null;
                string? displayName = null;
                string? rsvpStatus = null;
                string? note = null;
                if (i.UserId is Guid uid && inviteeUsersById.TryGetValue(uid, out var u))
                {
                    fullName = u.FullName;
                    displayName = u.DisplayName;
                }
                if (i.UserId is Guid uid2 && rsvpByUser.TryGetValue(uid2, out var r))
                {
                    rsvpStatus = r.Status.ToString();
                    note = r.Note;
                }
                guestList.Add(new GuestSummary(i.UserId, i.InvitedEmail, fullName, displayName, rsvpStatus, note));
            }
            guests = guestList;
        }

        return new GetEventResponse(
            @event.Id,
            @event.Title,
            @event.StartsAtUtc,
            @event.EndsAtUtc,
            @event.Location,
            @event.Description,
            @event.Status.ToString(),
            @event.AllowPlusOne,
            @event.ShowGuestList,
            host,
            isHost,
            myRsvp?.Status.ToString(),
            goingCount,
            maybeCount,
            cantGoCount,
            guests);
    }
}
