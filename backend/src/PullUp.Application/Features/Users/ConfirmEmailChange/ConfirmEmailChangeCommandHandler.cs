using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITokenHasher _tokenHasher;

    public ConfirmEmailChangeCommandHandler(
        IAppDbContext db,
        ICurrentUserAccessor currentUser,
        ITokenHasher tokenHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _tokenHasher = tokenHasher;
    }

    public async Task<Unit> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user no longer exists.");

        var hash = _tokenHasher.Hash(request.Token);
        if (!user.TryConfirmEmailChange(hash, DateTimeOffset.UtcNow))
        {
            throw new InvalidEmailChangeTokenException();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
