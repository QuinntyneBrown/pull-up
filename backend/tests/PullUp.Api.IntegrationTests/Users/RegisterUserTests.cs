// Acceptance Test
// Traces to: L2-001 (register with valid input), L2-002 (duplicate email), L2-003 (password complexity)
// Description: HTTP -> MediatR -> FluentValidation -> handler -> EF -> SQLite round-trip for the
// RegisterUser sample slice. This is the MB1 MVP's reference end-to-end test.

using System.Net;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.RegisterUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class RegisterUserTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RegisterUserTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_with_valid_input_returns_201_and_access_token()
    {
        var request = new
        {
            fullName = "Rosa Marquez",
            email = $"rosa.{Guid.NewGuid():N}@example.com",
            password = "Hunter2!secret"
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.UserId);
        Assert.Equal(request.email, body.Email);
        Assert.Equal("Rosa Marquez", body.FullName);
        Assert.Equal("Rosa", body.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        var email = $"dup.{Guid.NewGuid():N}@example.com";
        var first = new { fullName = "First Person",  email, password = "Hunter2!secret" };
        var second = new { fullName = "Second Person", email, password = "Hunter2!secret" };

        var firstResponse = await _client.PostAsJsonAsync("/api/users", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/users", second);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400_with_validation_problem()
    {
        var request = new
        {
            fullName = "Weak Password User",
            email = $"weak.{Guid.NewGuid():N}@example.com",
            password = "short"
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(problem.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Register_with_missing_full_name_returns_400()
    {
        var request = new
        {
            fullName = "",
            email = $"noname.{Guid.NewGuid():N}@example.com",
            password = "Hunter2!secret"
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
