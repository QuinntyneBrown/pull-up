// Acceptance Test
// Traces to: L2-014 (delete account requires re-typed password),
// L2-015 (tombstone fields; hosted future events cancelled; invitee links removed; refresh revoked).

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Domain.Events;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class DeleteAccountTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DeleteAccountTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, Guid userId, string email, SignInUserResponse signIn)> SignedInAsync(string prefix, string password)
    {
        var client = _factory.CreateClient();
        var email = $"{prefix}.{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/users", new { fullName = $"{prefix} User", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.UserId, email, body);
    }

    [Fact]
    public async Task Wrong_password_returns_401_and_user_not_deleted()
    {
        var (client, userId, _, _) = await SignedInAsync("delete-wrong", "Hunter2!secret");

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { currentPassword = "wrong" }),
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.False(user.FullName.StartsWith("[deleted"));
    }

    [Fact]
    public async Task Correct_password_tombstones_user_revokes_refresh_and_cancels_hosted_future_events()
    {
        const string password = "Hunter2!secret";
        var (client, userId, originalEmail, signIn) = await SignedInAsync("delete-ok", password);

        // Host a future event so we can verify cancellation on delete.
        var create = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Will Be Cancelled",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var eventId = (await create.Content.ReadFromJsonAsync<CreateEventResponse>())!.Id;

        var delete = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { currentPassword = password }),
        });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.StartsWith("[deleted", user.FullName);
        Assert.StartsWith("[deleted", user.DisplayName);
        Assert.NotEqual(originalEmail, user.Email);
        Assert.NotNull(user.DeletedAt);

        var hostedEvent = await db.Events.SingleAsync(e => e.Id == eventId);
        Assert.Equal(EventStatus.Cancelled, hostedEvent.Status);

        // Refresh tokens revoked: the original refresh token can't mint a new pair.
        var refreshResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = signIn.RefreshToken,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Deleting_invitee_user_removes_them_from_active_invitations_on_future_events()
    {
        const string password = "Hunter2!secret";
        var (host, _, _, _) = await SignedInAsync("delete-inv-host", password);
        var (invitee, inviteeId, inviteeEmail, _) = await SignedInAsync("delete-inv-invitee", password);

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Future With Invitee",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var eventId = (await create.Content.ReadFromJsonAsync<CreateEventResponse>())!.Id;

        var delete = await invitee.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { currentPassword = password }),
        });
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invitation = await db.Invitations.SingleAsync(i => i.EventId == eventId && i.UserId == inviteeId);
        Assert.NotNull(invitation.RemovedAt);
    }
}
