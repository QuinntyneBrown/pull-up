namespace PullUp.Domain.Users;

public sealed class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }

    public void MarkUsed(DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            return;
        }
        UsedAt = now;
    }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
