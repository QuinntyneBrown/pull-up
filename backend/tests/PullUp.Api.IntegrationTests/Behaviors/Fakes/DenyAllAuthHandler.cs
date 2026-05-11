using PullUp.Application.Common.Authorization;

namespace PullUp.Api.IntegrationTests.Behaviors.Fakes;

public sealed class DenyAllAuthHandler : IAuthorizationHandler<FakeAuthRequest>
{
    public Task<bool> AuthorizeAsync(FakeAuthRequest request, CancellationToken cancellationToken) => Task.FromResult(false);
}
