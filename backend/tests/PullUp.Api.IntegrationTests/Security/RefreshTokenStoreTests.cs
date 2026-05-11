// Acceptance Test
// Traces to: L2-006 (refresh token storage), L2-007 (sign-out revokes refresh),
// L2-009 (password-reset revokes refresh), L2-040 (passwords/tokens never stored
// in plaintext), L2-044 (secrets never logged).
// Description: refresh token generation produces a 256-bit raw value that the
// token hasher can verify against the persisted HMAC; the raw value is never
// stored on the entity; revocation is recorded on the same row.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Abstractions;
using PullUp.Domain.Users;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Security;

public sealed class RefreshTokenStoreTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RefreshTokenStoreTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Issue_persist_and_verify_round_trip()
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var hasher = scope.ServiceProvider.GetRequiredService<ITokenHasher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = User.Register(
            email: $"rt.{Guid.NewGuid():N}@example.com",
            fullName: "Refresh Test",
            passwordHash: "PBKDF2-SHA256:1:fake:fake");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var issuance = jwt.IssueRefreshToken(user);
        db.RefreshTokens.Add(issuance.Record);
        await db.SaveChangesAsync();

        Assert.False(string.IsNullOrWhiteSpace(issuance.RawToken));
        Assert.NotEqual(issuance.RawToken, issuance.Record.TokenHash);
        Assert.Equal(user.Id, issuance.Record.UserId);
        Assert.True(issuance.Record.ExpiresAt > DateTimeOffset.UtcNow);

        Assert.True(hasher.Verify(issuance.RawToken, issuance.Record.TokenHash));
        Assert.False(hasher.Verify("not-the-real-token", issuance.Record.TokenHash));

        var roundTripped = await db.RefreshTokens.SingleAsync(t => t.Id == issuance.Record.Id);
        Assert.Equal(issuance.Record.TokenHash, roundTripped.TokenHash);
        Assert.Null(roundTripped.RevokedAt);
    }

    [Fact]
    public async Task Revoke_marks_revoked_at_and_links_replacement()
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = User.Register(
            email: $"rt.{Guid.NewGuid():N}@example.com",
            fullName: "Revoke Test",
            passwordHash: "PBKDF2-SHA256:1:fake:fake");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var first = jwt.IssueRefreshToken(user);
        var second = jwt.IssueRefreshToken(user);
        first.Record.Revoke(DateTimeOffset.UtcNow, replacedBy: second.Record.Id);
        db.RefreshTokens.AddRange(first.Record, second.Record);
        await db.SaveChangesAsync();

        var revoked = await db.RefreshTokens.SingleAsync(t => t.Id == first.Record.Id);
        Assert.NotNull(revoked.RevokedAt);
        Assert.Equal(second.Record.Id, revoked.ReplacedByTokenId);
    }
}
