// Acceptance Test
// Traces to: L2-006 (session persistence via refresh-token rotation).
// Description: sign in -> swap refresh -> new pair works; the old refresh is
// revoked and cannot be reused; invalid refresh tokens return 401.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.RefreshAccessToken;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class RefreshAccessTokenTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RefreshAccessTokenTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    private async Task<SignInUserResponse> SignInAsync()
    {
        var email = $"refresh.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Refresh Test", email, password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var signIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        Assert.NotNull(body);
        return body!;
    }

    [Fact]
    public async Task Refresh_swaps_tokens_and_new_access_works()
    {
        var initial = await SignInAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = initial.RefreshToken,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RefreshAccessTokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.NotEqual(initial.AccessToken, body.AccessToken);
        Assert.NotEqual(initial.RefreshToken, body.RefreshToken);

        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await authed.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Old_refresh_token_cannot_be_reused_after_rotation()
    {
        var initial = await SignInAsync();

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = initial.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var replay = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = initial.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Unknown_refresh_token_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "not-a-real-refresh-token-just-some-bytes",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
