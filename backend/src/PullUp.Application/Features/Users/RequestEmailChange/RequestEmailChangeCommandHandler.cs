using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Users.SignInUser;

namespace PullUp.Application.Features.Users.RequestEmailChange;

public sealed class RequestEmailChangeCommandHandler : IRequestHandler<RequestEmailChangeCommand, Unit>
{
    private const int TokenByteLength = 32;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;

    public RequestEmailChangeCommandHandler(
        IAppDbContext db,
        ICurrentUserAccessor currentUser,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IEmailSender emailSender)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
    }

    public async Task<Unit> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user no longer exists.");

        // Re-typed password gate (L2-013). Failure is HTTP 401 via the existing
        // InvalidCredentialsException mapping — same exception type as sign-in so
        // the wire format is consistent.
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength));
        var hash = _tokenHasher.Hash(raw);
        var expires = DateTimeOffset.UtcNow.Add(TokenLifetime);

        user.RequestEmailChange(request.NewEmail, hash, expires);
        await _db.SaveChangesAsync(cancellationToken);
        await _emailSender.SendEmailChangeVerificationAsync(user.PendingEmail!, raw, expires, cancellationToken);
        return Unit.Value;
    }
}
