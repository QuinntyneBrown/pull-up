using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.SignOut;

public sealed class SignOutCommandHandler : IRequestHandler<SignOutCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ITokenHasher _tokenHasher;

    public SignOutCommandHandler(IAppDbContext db, ITokenHasher tokenHasher)
    {
        _db = db;
        _tokenHasher = tokenHasher;
    }

    public async Task<Unit> Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: an unknown or already-revoked token still completes successfully.
        // The audit row captures the attempt either way.
        var hash = _tokenHasher.Hash(request.RefreshToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.Revoke(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }
}
