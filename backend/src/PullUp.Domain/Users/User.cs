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
}
