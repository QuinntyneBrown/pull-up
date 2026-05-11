using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PullUp.Application.Abstractions;
using PullUp.Domain.Users;

namespace PullUp.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private const int RefreshTokenByteLength = 32; // 256 bits

    private readonly JwtOptions _options;
    private readonly JsonWebTokenHandler _handler;
    private readonly ITokenHasher _tokenHasher;

    public JwtTokenService(IOptions<JwtOptions> options, ITokenHasher tokenHasher)
    {
        _options = options.Value;
        _tokenHasher = tokenHasher;
        _handler = new JsonWebTokenHandler();
    }

    public RefreshTokenIssuance IssueRefreshToken(User user)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var hash = _tokenHasher.Hash(raw);
        var record = RefreshToken.Issue(
            userId: user.Id,
            tokenHash: hash,
            now: DateTimeOffset.UtcNow,
            lifetime: TimeSpan.FromDays(_options.RefreshTokenLifetimeDays));
        return new RefreshTokenIssuance(raw, record);
    }

    public AccessToken Issue(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            })
        };

        var token = _handler.CreateToken(descriptor);
        return new AccessToken(token, expires);
    }
}
