// Acceptance Test
// Traces to: L2-003 (password complexity at reset), L2-009 (complete password
// reset: token validation + revoke all refresh tokens), L2-010 (audit).
// Description: register, request reset (captures raw token via CapturingEmailSender),
// complete with new password, sign in with new password, old refresh now revoked.

using System.Net;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class CompletePasswordResetTests : IClassFixture<CapturingEmailFactory>
{
    private readonly CapturingEmailFactory _factory;
    private readonly HttpClient _client;

    public CompletePasswordResetTests(CapturingEmailFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    private async Task<(string email, SignInUserResponse signIn)> SetupAccountAsync(string password)
    {
        var email = $"reset-complete.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Reset Complete Test", email, password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var signIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        return (email, (await signIn.Content.ReadFromJsonAsync<SignInUserResponse>())!);
    }

    private async Task<string> RequestResetAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/password-reset", new { email });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var token = _factory.Email.LastResetRawToken;
        Assert.False(string.IsNullOrEmpty(token));
        return token!;
    }

    [Fact]
    public async Task Valid_token_with_compliant_password_rotates_credentials_and_revokes_refresh()
    {
        var (email, original) = await SetupAccountAsync("OldPassword1!");
        var rawResetToken = await RequestResetAsync(email);
        const string newPassword = "BrandNewPwd9$";

        var confirm = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = rawResetToken,
            newPassword,
        });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        // Old password no longer works.
        var oldSignIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password = "OldPassword1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldSignIn.StatusCode);

        // New password works.
        var newSignIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newSignIn.StatusCode);

        // Pre-reset refresh token is now revoked.
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = original.RefreshToken,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Reused_reset_token_returns_400()
    {
        var (email, _) = await SetupAccountAsync("OldPassword1!");
        var rawResetToken = await RequestResetAsync(email);

        var first = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = rawResetToken,
            newPassword = "AnotherPwd9!",
        });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var replay = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = rawResetToken,
            newPassword = "ThirdPwd9!",
        });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task Weak_new_password_returns_400_without_changing_anything()
    {
        var (email, _) = await SetupAccountAsync("OldPassword1!");
        var rawResetToken = await RequestResetAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            token = rawResetToken,
            newPassword = "short",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Old password still works.
        var oldSignIn = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password = "OldPassword1!" });
        Assert.Equal(HttpStatusCode.OK, oldSignIn.StatusCode);
    }
}
