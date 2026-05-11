using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, RefreshAccessTokenResponse>
{
    private readonly IAppDbContext _db;
    private readonly ITokenHasher _tokenHasher;
    private readonly IJwtTokenService _tokens;

    public RefreshAccessTokenCommandHandler(
        IAppDbContext db,
        ITokenHasher tokenHasher,
        IJwtTokenService tokens)
    {
        _db = db;
        _tokenHasher = tokenHasher;
        _tokens = tokens;
    }

    public async Task<RefreshAccessTokenResponse> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenHasher.Hash(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is null || !existing.IsActive(now))
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var newAccess = _tokens.Issue(user);
        var newRefresh = _tokens.IssueRefreshToken(user);

        existing.Revoke(now, replacedBy: newRefresh.Record.Id);
        _db.RefreshTokens.Add(newRefresh.Record);
        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshAccessTokenResponse(
            newAccess.Value,
            newAccess.ExpiresAt,
            newRefresh.RawToken,
            newRefresh.Record.ExpiresAt);
    }
}
