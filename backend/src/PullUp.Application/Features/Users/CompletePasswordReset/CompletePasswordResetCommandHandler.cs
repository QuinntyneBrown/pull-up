using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.CompletePasswordReset;

public sealed class CompletePasswordResetCommandHandler : IRequestHandler<CompletePasswordResetCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ITokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;

    public CompletePasswordResetCommandHandler(
        IAppDbContext db,
        ITokenHasher tokenHasher,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
    }

    public async Task<Unit> Handle(CompletePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = _tokenHasher.Hash(request.Token);

        var record = await _db.PasswordResetTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (record is null || !record.IsActive(now))
        {
            throw new InvalidPasswordResetTokenException();
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == record.UserId, cancellationToken)
            ?? throw new InvalidPasswordResetTokenException();

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(newHash, now);
        record.MarkUsed(now);

        // Revoke every active refresh token for this user — completing a reset
        // forces all existing sessions to re-authenticate (L2-009).
        var activeRefresh = await _db.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var r in activeRefresh)
        {
            r.Revoke(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
