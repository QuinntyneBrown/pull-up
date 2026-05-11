using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.CancelEvent;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Domain.Events;

namespace PullUp.Application.Features.Users.DeleteAccount;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISender _mediator;

    public DeleteAccountCommandHandler(
        IAppDbContext db,
        ICurrentUserAccessor currentUser,
        IPasswordHasher passwordHasher,
        ISender mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user no longer exists.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var now = DateTimeOffset.UtcNow;

        // 1) Cancel every future event hosted by this user — reuse the per-event
        //    CancelEvent handler so cancellation notifications fan out per L2-029.
        //    Filter on identity in SQL, narrow by date client-side (SQLite cannot
        //    translate DateTimeOffset comparisons reliably; SQL Server can).
        var hostedScheduled = await _db.Events
            .Where(e => e.HostId == userId && e.Status == EventStatus.Scheduled)
            .Select(e => new { e.Id, e.StartsAtUtc })
            .ToListAsync(cancellationToken);
        var hostedFutureEventIds = hostedScheduled
            .Where(e => e.StartsAtUtc > now)
            .Select(e => e.Id)
            .ToList();
        foreach (var eventId in hostedFutureEventIds)
        {
            await _mediator.Send(new CancelEventCommand(eventId), cancellationToken);
        }

        // 2) Remove the user from active invitations on future events. Same
        //    client-side date filter pattern.
        var invitationsWithStart = await (
            from i in _db.Invitations
            join e in _db.Events on i.EventId equals e.Id
            where i.UserId == userId && i.RemovedAt == null
            select new { Invitation = i, e.StartsAtUtc }
        ).ToListAsync(cancellationToken);
        foreach (var pair in invitationsWithStart.Where(p => p.StartsAtUtc > now))
        {
            pair.Invitation.Remove(now);
        }

        // 3) Revoke all active refresh tokens for this user.
        var activeRefresh = await _db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var r in activeRefresh)
        {
            r.Revoke(now);
        }

        // 4) Tombstone identifying fields on the User row itself.
        user.Tombstone(now);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
