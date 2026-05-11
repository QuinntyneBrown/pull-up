using PullUp.Domain.Users;

namespace PullUp.Application.Abstractions;

public interface IJwtTokenService
{
    AccessToken Issue(User user);

    RefreshTokenIssuance IssueRefreshToken(User user);
}
