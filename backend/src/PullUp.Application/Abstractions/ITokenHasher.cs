namespace PullUp.Application.Abstractions;

// Stable, server-side hash for opaque tokens (refresh tokens, password-reset
// tokens, email-verification tokens). Uses a server-side pepper from config so
// stolen DB rows are not enough to reverse a token; the same raw value always
// produces the same hash so lookups are O(1).
public interface ITokenHasher
{
    string Hash(string rawToken);

    bool Verify(string rawToken, string storedHash);
}
