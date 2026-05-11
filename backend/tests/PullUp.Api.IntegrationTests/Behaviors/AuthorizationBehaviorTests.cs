// Acceptance Test
// Traces to: L2-027 (non-host cannot edit -> 403), L2-045 (User role limits ops), L2-046 (Admin role).
// Description: AuthorizationBehavior gates command pipeline by resolving registered
// IAuthorizationHandler<TRequest> instances; if any returns false, NotAuthorizedException
// is thrown and the API surface maps it to HTTP 403. This test exercises the behavior in
// isolation with fake requests + fake handlers so the rule is provable without a real
// HTTP-protected command yet existing.

using MediatR;
using PullUp.Api.IntegrationTests.Behaviors.Fakes;
using PullUp.Application.Behaviors;
using PullUp.Application.Common.Authorization;
using PullUp.Application.Common.Exceptions;
using Xunit;

namespace PullUp.Api.IntegrationTests.Behaviors;

public sealed class AuthorizationBehaviorTests
{
    private static Task<Unit> Continuation(CancellationToken _) => Task.FromResult(Unit.Value);

    [Fact]
    public async Task Passes_when_no_auth_handlers_are_registered()
    {
        var behavior = new AuthorizationBehavior<FakeAuthRequest, Unit>(
            Array.Empty<IAuthorizationHandler<FakeAuthRequest>>());

        var result = await behavior.Handle(
            new FakeAuthRequest(),
            Continuation,
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Passes_when_every_auth_handler_returns_true()
    {
        var behavior = new AuthorizationBehavior<FakeAuthRequest, Unit>(
            new IAuthorizationHandler<FakeAuthRequest>[]
            {
                new AllowAllAuthHandler(),
                new AllowAllAuthHandler(),
            });

        var result = await behavior.Handle(
            new FakeAuthRequest(),
            Continuation,
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Throws_NotAuthorized_when_any_auth_handler_returns_false()
    {
        var behavior = new AuthorizationBehavior<FakeAuthRequest, Unit>(
            new IAuthorizationHandler<FakeAuthRequest>[]
            {
                new AllowAllAuthHandler(),
                new DenyAllAuthHandler(),
            });

        await Assert.ThrowsAsync<NotAuthorizedException>(() =>
            behavior.Handle(
                new FakeAuthRequest(),
                Continuation,
                CancellationToken.None));
    }
}
