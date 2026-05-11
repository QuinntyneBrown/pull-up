using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Users;

namespace PullUp.Application.Features.Users.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    private const int TokenByteLength = 32; // 256 bits
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);

    private readonly IAppDbContext _db;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;

    public RequestPasswordResetCommandHandler(
        IAppDbContext db,
        ITokenHasher tokenHasher,
        IEmailSender emailSender)
    {
        _db = db;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Unknown email -> no-op so the response is indistinguishable from the
        // known-email path (L2-008 no-enumeration). The audit row still records
        // that a reset was requested for an email; the row simply lacks a user
        // link in that case.
        if (user is null)
        {
            return Unit.Value;
        }

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength));
        var hash = _tokenHasher.Hash(raw);
        var token = PasswordResetToken.Issue(user.Id, hash, DateTimeOffset.UtcNow, TokenLifetime);

        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);

        await _emailSender.SendPasswordResetEmailAsync(user.Email, raw, token.ExpiresAt, cancellationToken);
        return Unit.Value;
    }
}
