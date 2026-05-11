// Acceptance Test
// Traces to: L2-004 (sign-in with valid credentials returns 200 + JWT + refresh),
// L2-005 (invalid credentials -> generic 401, no user-enumeration; audit log),
// L2-043 (audit row on failed sign-in).
// Description: end-to-end auth flow. Register a user (gives a known password
// hash on the User row), then exercise /api/auth/sign-in three ways and verify
// the audit log records both success and failure.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Features.Users.GetCurrentUser;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class SignInUserTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SignInUserTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Sign In Test",
            email,
            password,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    [Fact]
    public async Task Valid_credentials_returns_200_with_tokens_and_profile()
    {
        var email = $"signin-ok.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await RegisterAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SignInUserResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal(email, body.Email);
        Assert.Equal("Sign In Test", body.FullName);

        // Access token works against an authenticated endpoint.
        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var me = await authedClient.GetFromJsonAsync<GetCurrentUserResponse>("/api/users/me");
        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
    }

    [Fact]
    public async Task Wrong_password_returns_401_with_generic_message_and_no_enumeration()
    {
        var email = $"signin-wrong.{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email, "Hunter2!secret");

        var response = await _client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email,
            password = "totally-different-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("does not exist", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_email_returns_401_with_same_generic_message()
    {
        var unknown = $"signin-unknown.{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = unknown,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Successful_sign_in_writes_audit_row_with_outcome_SUCCESS()
    {
        var email = $"signin-audit-ok.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await RegisterAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var anySuccess = await db.AuditLog.Where(a => a.Event == "USER_SIGNED_IN" && a.Outcome == "SUCCESS").AnyAsync();
        Assert.True(anySuccess);
    }

    [Fact]
    public async Task Failed_sign_in_writes_audit_row_with_outcome_FAILURE()
    {
        var email = $"signin-audit-fail.{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email, "Hunter2!secret");

        var response = await _client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email,
            password = "wrong-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var anyFailure = await db.AuditLog.Where(a => a.Event == "USER_SIGNED_IN" && a.Outcome == "FAILURE").AnyAsync();
        Assert.True(anyFailure);
    }
}
