namespace PullUp.Domain.Users;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }

    public void Revoke(DateTimeOffset now, Guid? replacedBy = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }
        RevokedAt = now;
        ReplacedByTokenId = replacedBy;
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
