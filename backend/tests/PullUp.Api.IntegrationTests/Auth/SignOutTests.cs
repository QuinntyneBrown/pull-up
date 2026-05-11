// Acceptance Test
// Traces to: L2-007 (sign-out clears session — revokes the supplied refresh token).
// Description: sign in -> sign out with the refresh -> the refresh is now revoked
// and the /api/auth/refresh endpoint rejects subsequent attempts to use it.

using System.Net;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class SignOutTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SignOutTests(TestWebApplicationFactory factory)
    {
        factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    private async Task<SignInUserResponse> SignInAsync()
    {
        var email = $"signout.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Sign Out Test", email, password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var signIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        return (await signIn.Content.ReadFromJsonAsync<SignInUserResponse>())!;
    }

    [Fact]
    public async Task Sign_out_revokes_the_supplied_refresh_token_and_refresh_then_returns_401()
    {
        var initial = await SignInAsync();

        var signOut = await _client.PostAsJsonAsync("/api/auth/sign-out", new
        {
            refreshToken = initial.RefreshToken,
        });
        Assert.True(signOut.IsSuccessStatusCode, $"sign-out should succeed; got {signOut.StatusCode}");

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = initial.RefreshToken,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Sign_out_is_idempotent_for_an_unknown_or_already_revoked_token()
    {
        // Unknown token — sign-out should still succeed (idempotent, no enumeration).
        var unknown = await _client.PostAsJsonAsync("/api/auth/sign-out", new
        {
            refreshToken = "not-a-real-token",
        });
        Assert.True(unknown.IsSuccessStatusCode, $"sign-out for unknown token should succeed; got {unknown.StatusCode}");

        // Sign in, sign out, sign out again — second sign-out should also succeed.
        var initial = await SignInAsync();
        var first = await _client.PostAsJsonAsync("/api/auth/sign-out", new { refreshToken = initial.RefreshToken });
        Assert.True(first.IsSuccessStatusCode);
        var second = await _client.PostAsJsonAsync("/api/auth/sign-out", new { refreshToken = initial.RefreshToken });
        Assert.True(second.IsSuccessStatusCode);
    }
}
