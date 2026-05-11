using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.SignInUser;

public sealed class SignInUserCommandHandler : IRequestHandler<SignInUserCommand, SignInUserResponse>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly ISignInRateLimiter _rateLimiter;

    public SignInUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        ISignInRateLimiter rateLimiter)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _rateLimiter = rateLimiter;
    }

    public async Task<SignInUserResponse> Handle(SignInUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Throws SignInRateLimitExceededException if the email is currently locked.
        _rateLimiter.EnsureNotLocked(normalizedEmail);

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Throw the SAME exception for unknown email and wrong password so the
        // response shape and timing class are identical (L2-005).
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            _rateLimiter.RegisterFailedAttempt(normalizedEmail);
            throw new InvalidCredentialsException();
        }

        _rateLimiter.ResetAttempts(normalizedEmail);

        var access = _tokens.Issue(user);
        var refresh = _tokens.IssueRefreshToken(user);

        _db.RefreshTokens.Add(refresh.Record);
        await _db.SaveChangesAsync(cancellationToken);

        return new SignInUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.DisplayName,
            user.Role.ToString(),
            access.Value,
            access.ExpiresAt,
            refresh.RawToken,
            refresh.Record.ExpiresAt);
    }
}
