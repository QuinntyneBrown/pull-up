// Acceptance Test
// Traces to: L2-012 (edit display name and full name).
// Description: signed-in user can update full name + display name; /me reflects
// the change; empty / over-length values return 400 with validation details.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class UpdateProfileTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateProfileTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"profile.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        var register = await client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Original Name", email, password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Valid_update_changes_full_name_and_display_name_and_me_reflects_it()
    {
        var client = await AuthedClientAsync();

        var response = await client.PutAsJsonAsync("/api/users/me/profile", new
        {
            fullName = "Updated Name",
            displayName = "Updated",
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var me = await client.GetFromJsonAsync<GetCurrentUserResponse>("/api/users/me");
        Assert.NotNull(me);
        Assert.Equal("Updated Name", me!.FullName);
        Assert.Equal("Updated", me.DisplayName);
    }

    [Fact]
    public async Task Empty_full_name_returns_400()
    {
        var client = await AuthedClientAsync();

        var response = await client.PutAsJsonAsync("/api/users/me/profile", new
        {
            fullName = "",
            displayName = "Whatever",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Over_length_full_name_returns_400()
    {
        var client = await AuthedClientAsync();

        var response = await client.PutAsJsonAsync("/api/users/me/profile", new
        {
            fullName = new string('A', 101),
            displayName = "Whatever",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me/profile", new
        {
            fullName = "Anon",
            displayName = "Anon",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
