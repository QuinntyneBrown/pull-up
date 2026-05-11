// Acceptance Test
// Traces to: L2-011 (view own profile), L2-041 (JWT validation)
// Description: GET /api/users/me requires a valid JWT issued by the same API; without it,
// returns 401. With a token from register, returns the current user record.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.RegisterUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class GetCurrentUserTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GetCurrentUserTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_token_from_register_returns_200_and_user_profile()
    {
        var client = _factory.CreateClient();

        var registerRequest = new
        {
            fullName = "Luis Marquez",
            email = $"luis.{Guid.NewGuid():N}@example.com",
            password = "Hunter2!secret"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/users", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();
        Assert.NotNull(registered);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);

        var meResponse = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<GetCurrentUserResponse>();
        Assert.NotNull(me);
        Assert.Equal(registered.UserId, me!.UserId);
        Assert.Equal(registered.Email, me.Email);
        Assert.Equal(registered.FullName, me.FullName);
        Assert.Equal("User", me.Role);
    }
}
