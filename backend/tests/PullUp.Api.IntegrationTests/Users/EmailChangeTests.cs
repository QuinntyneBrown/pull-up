// Acceptance Test
// Traces to: L2-013 (request email change requires current password; verification
// link confirms the change; primary email only updates on confirm).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Api.IntegrationTests.Auth;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class EmailChangeTests : IClassFixture<CapturingEmailFactory>
{
    private readonly CapturingEmailFactory _factory;

    public EmailChangeTests(CapturingEmailFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, string originalEmail)> RegisterAndAuthAsync(string password)
    {
        var client = _factory.CreateClient();
        var email = $"emailchange.{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Email Change Test", email, password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, email);
    }

    [Fact]
    public async Task Request_with_wrong_current_password_returns_401()
    {
        var (client, _) = await RegisterAndAuthAsync("Hunter2!secret");

        var response = await client.PostAsJsonAsync("/api/users/me/email-change", new
        {
            newEmail = $"new.{Guid.NewGuid():N}@example.com",
            currentPassword = "wrong",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_with_correct_password_returns_202_and_captures_verification_token()
    {
        var (client, _) = await RegisterAndAuthAsync("Hunter2!secret");
        var newEmail = $"new.{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/users/me/email-change", new
        {
            newEmail,
            currentPassword = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        Assert.Equal(newEmail, _factory.Email.LastEmailChangeTargetEmail);
        Assert.False(string.IsNullOrEmpty(_factory.Email.LastEmailChangeRawToken));
    }

    [Fact]
    public async Task Confirm_with_captured_token_promotes_email_and_me_reflects_it()
    {
        var (client, originalEmail) = await RegisterAndAuthAsync("Hunter2!secret");
        var newEmail = $"new.{Guid.NewGuid():N}@example.com";

        var request = await client.PostAsJsonAsync("/api/users/me/email-change", new
        {
            newEmail,
            currentPassword = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var token = _factory.Email.LastEmailChangeRawToken!;

        var confirm = await client.PostAsJsonAsync("/api/users/me/email-change/confirm", new
        {
            token,
        });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var me = await client.GetFromJsonAsync<GetCurrentUserResponse>("/api/users/me");
        Assert.NotNull(me);
        Assert.Equal(newEmail, me!.Email);
        Assert.NotEqual(originalEmail, me.Email);
    }

    [Fact]
    public async Task Confirm_with_stale_token_returns_400()
    {
        var (client, _) = await RegisterAndAuthAsync("Hunter2!secret");

        var confirm = await client.PostAsJsonAsync("/api/users/me/email-change/confirm", new
        {
            token = "not-a-real-token",
        });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }
}
