using MediatR;
using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;

namespace PullUp.Application.Features.Users.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetCurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user no longer exists.");

        return new GetCurrentUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.DisplayName,
            user.Role.ToString(),
            user.CreatedAt);
    }
}
