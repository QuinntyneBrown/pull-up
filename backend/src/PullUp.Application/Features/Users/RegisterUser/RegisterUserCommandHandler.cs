using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Users;

namespace PullUp.Application.Features.Users.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;

    public RegisterUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        var passwordHash = _hasher.Hash(request.Password);
        var user = User.Register(normalizedEmail, request.FullName, passwordHash);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokens.Issue(user);

        return new RegisterUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.DisplayName,
            token.Value,
            token.ExpiresAt);
    }
}
