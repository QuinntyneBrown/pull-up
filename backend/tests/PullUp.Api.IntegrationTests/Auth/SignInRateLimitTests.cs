// Acceptance Test
// Traces to: L2-042 (5 failed sign-in attempts per email per 60s -> HTTP 429 with Retry-After: 60).

using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class SignInRateLimitTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SignInRateLimitTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Five_wrong_passwords_then_a_sixth_returns_429_with_Retry_After_60()
    {
        // Register a real user so the 401s come from password-mismatch (not unknown email,
        // which behaves identically per L2-005 anyway, but this keeps the intent explicit).
        var email = $"ratelimit.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Rate Limit Test",
            email,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // First five wrong-password attempts return 401.
        for (var i = 0; i < 5; i++)
        {
            var attempt = await _client.PostAsJsonAsync("/api/auth/sign-in", new
            {
                email,
                password = $"wrong-{i}",
            });
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        // The sixth attempt is locked out.
        var blocked = await _client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email,
            password = "wrong-final",
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var retryAfter = blocked.Headers.RetryAfter;
        Assert.NotNull(retryAfter);
        Assert.Equal(TimeSpan.FromSeconds(60), retryAfter!.Delta);
    }

    [Fact]
    public async Task A_single_failed_attempt_does_not_lock_a_subsequent_correct_password()
    {
        var email = $"ratelimit-recover.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Recovery Test",
            email,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var bad = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var good = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password = "Hunter2!secret" });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
    }
}
