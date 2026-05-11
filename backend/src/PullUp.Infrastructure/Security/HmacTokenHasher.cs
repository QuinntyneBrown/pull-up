using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PullUp.Application.Abstractions;

namespace PullUp.Infrastructure.Security;

// HMAC-SHA-256 with a server-side pepper. Suitable for short opaque tokens that
// are already high-entropy (256 bits); no per-row salt is needed because the
// pepper provides defense against pre-computed tables and the hash is
// deterministic so the DB can index on it.
public sealed class HmacTokenHasher : ITokenHasher
{
    private readonly byte[] _pepper;

    public HmacTokenHasher(IOptions<JwtOptions> options)
    {
        var pepper = options.Value.TokenHasherPepper;
        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException(
                "Configuration value 'Jwt:TokenHasherPepper' is required and must be at least 32 bytes.");
        }
        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawToken);
        using var hmac = new HMACSHA256(_pepper);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string rawToken, string storedHash)
    {
        if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        byte[] storedBytes;
        try
        {
            storedBytes = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(_pepper);
        var actual = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return CryptographicOperations.FixedTimeEquals(storedBytes, actual);
    }
}
