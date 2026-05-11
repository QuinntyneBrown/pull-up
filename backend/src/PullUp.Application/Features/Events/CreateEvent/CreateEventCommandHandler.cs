using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Events.CreateEvent;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, CreateEventResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateEventCommandHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreateEventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var hostId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var now = DateTimeOffset.UtcNow;

        var @event = Event.Create(
            hostId: hostId,
            title: request.Title,
            startsAtUtc: request.StartsAtUtc,
            endsAtUtc: request.EndsAtUtc,
            location: request.Location,
            description: request.Description ?? string.Empty,
            allowPlusOne: request.AllowPlusOne,
            showGuestList: request.ShowGuestList,
            now: now);

        _db.Events.Add(@event);

        // Host self-RSVP = Going (L2-019).
        _db.Rsvps.Add(Rsvp.Create(@event.Id, hostId, RsvpStatus.Going, note: null, now));

        // Resolve invitee emails to user ids where they match an existing account;
        // email-only invitations link to the user later (BT-021 / pending account-link).
        if (request.InviteeEmails.Count > 0)
        {
            var normalized = request.InviteeEmails
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var matchedUsers = await _db.Users
                .Where(u => normalized.Contains(u.Email))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(cancellationToken);
            var matchedByEmail = matchedUsers.ToDictionary(u => u.Email, u => u.Id);

            foreach (var email in normalized)
            {
                matchedByEmail.TryGetValue(email, out var userId);
                _db.Invitations.Add(Invitation.Create(
                    @event.Id,
                    userId == Guid.Empty ? null : userId,
                    email,
                    now));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CreateEventResponse(@event.Id);
    }
}
