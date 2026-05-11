using PullUp.Application.Common.Authorization;

namespace PullUp.Api.IntegrationTests.Behaviors.Fakes;

public sealed class AllowAllAuthHandler : IAuthorizationHandler<FakeAuthRequest>
{
    public Task<bool> AuthorizeAsync(FakeAuthRequest request, CancellationToken cancellationToken) => Task.FromResult(true);
}
