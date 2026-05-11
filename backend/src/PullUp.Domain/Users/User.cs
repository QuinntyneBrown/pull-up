namespace PullUp.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Role Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastPasswordChangedAt { get; private set; }

    public string? PendingEmail { get; private set; }
    public string? PendingEmailTokenHash { get; private set; }
    public DateTimeOffset? PendingEmailExpiresAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    private User() { }

    public static User Register(string email, string fullName, string passwordHash)
    {
        var now = DateTimeOffset.UtcNow;
        var trimmedFullName = fullName.Trim();
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = trimmedFullName,
            DisplayName = trimmedFullName.Split(' ', 2)[0],
            PasswordHash = passwordHash,
            Role = Role.User,
            CreatedAt = now,
            LastPasswordChangedAt = now,
        };
    }

    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(newPasswordHash);
        PasswordHash = newPasswordHash;
        LastPasswordChangedAt = now;
    }

    public void UpdateProfile(string fullName, string displayName)
    {
        var trimmedFullName = fullName.Trim();
        var trimmedDisplayName = displayName.Trim();
        ArgumentException.ThrowIfNullOrEmpty(trimmedFullName);
        ArgumentException.ThrowIfNullOrEmpty(trimmedDisplayName);
        FullName = trimmedFullName;
        DisplayName = trimmedDisplayName;
    }

    public void RequestEmailChange(string newEmail, string tokenHash, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrEmpty(newEmail);
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        PendingEmail = newEmail.Trim().ToLowerInvariant();
        PendingEmailTokenHash = tokenHash;
        PendingEmailExpiresAt = expiresAt;
    }

    public bool TryConfirmEmailChange(string tokenHash, DateTimeOffset now)
    {
        if (PendingEmail is null || PendingEmailTokenHash is null || PendingEmailExpiresAt is null)
        {
            return false;
        }
        if (PendingEmailExpiresAt <= now)
        {
            return false;
        }
        if (!string.Equals(PendingEmailTokenHash, tokenHash, StringComparison.Ordinal))
        {
            return false;
        }

        Email = PendingEmail;
        PendingEmail = null;
        PendingEmailTokenHash = null;
        PendingEmailExpiresAt = null;
        return true;
    }

    public const string TombstoneMarker = "[deleted user]";

    public void Tombstone(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        FullName = TombstoneMarker;
        DisplayName = TombstoneMarker;
        // Keep the unique-email constraint satisfied while making the value useless.
        Email = $"deleted-{Id:N}@pullup.invalid";
        PasswordHash = string.Empty;
        PendingEmail = null;
        PendingEmailTokenHash = null;
        PendingEmailExpiresAt = null;
        DeletedAt = now;
    }
}
