using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateProfileCommandHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user no longer exists.");

        user.UpdateProfile(request.FullName, request.DisplayName);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
