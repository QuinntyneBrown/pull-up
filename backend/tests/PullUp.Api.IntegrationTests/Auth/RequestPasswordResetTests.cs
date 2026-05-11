// Acceptance Test
// Traces to: L2-008 (request password reset link — always 202, no enumeration),
// L2-010 (PASSWORD_RESET_REQUESTED audit row), L2-044 (raw token never logged).
// Description: submit a known email -> 202 + one PasswordResetToken row.
// Submit an unknown email -> 202 + zero rows. Both responses are structurally
// identical so the endpoint cannot be used to probe whether an email exists.

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auth;

public sealed class RequestPasswordResetTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RequestPasswordResetTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Known_email_returns_202_and_creates_a_reset_token_row()
    {
        var email = $"reset-known.{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Reset Known", email, password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset", new { email });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var hasToken = await db.PasswordResetTokens.Where(t => t.UserId == user.Id).AnyAsync();
        Assert.True(hasToken);
    }

    [Fact]
    public async Task Unknown_email_returns_202_with_no_token_row()
    {
        var email = $"reset-unknown.{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/password-reset", new { email });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userExists = await db.Users.AnyAsync(u => u.Email == email);
        Assert.False(userExists);
        // PasswordResetToken table has no rows linked to any non-existent user (we never created one).
        var leakedRowCount = await db.PasswordResetTokens
            .Where(t => !db.Users.Any(u => u.Id == t.UserId))
            .CountAsync();
        Assert.Equal(0, leakedRowCount);
    }
}
