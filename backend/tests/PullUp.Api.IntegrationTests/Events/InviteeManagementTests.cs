// Acceptance Test
// Traces to: L2-031 (add existing user as invitee + EventInvited notification),
// L2-032 (invite by email when no user exists), L2-033 (remove invitee clears RSVP),
// L2-037 (notification dispatch reused).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class InviteeManagementTests : IClassFixture<NotificationCapturingFactory>
{
    private readonly NotificationCapturingFactory _factory;

    public InviteeManagementTests(NotificationCapturingFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, Guid userId, string email)> AuthedClientAsync(string prefix)
    {
        var client = _factory.CreateClient();
        var email = $"{prefix}.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await client.PostAsJsonAsync("/api/users", new { fullName = $"{prefix} User", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.UserId, email);
    }

    private async Task<Guid> CreateEventAsync(HttpClient host, params string[] inviteeEmails)
    {
        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Invitee Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = inviteeEmails,
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<CreateEventResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task Host_adds_existing_user_creates_invitation_and_dispatches_EventInvited()
    {
        var (host, _, _) = await AuthedClientAsync("add-host");
        var (_, inviteeId, inviteeEmail) = await AuthedClientAsync("add-invitee");

        var eventId = await CreateEventAsync(host);

        _factory.Notifications.Sent.Clear();

        var response = await host.PostAsJsonAsync($"/api/events/{eventId}/invitees", new { email = inviteeEmail });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.Contains(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == inviteeId
                 && n.Kind == NotificationKind.EventInvited
                 && n.EventId == eventId);
    }

    [Fact]
    public async Task Host_adds_unknown_email_stores_email_only_invitation()
    {
        var (host, _, _) = await AuthedClientAsync("addunknown-host");
        var eventId = await CreateEventAsync(host);

        var unknownEmail = $"never-registered.{Guid.NewGuid():N}@example.com";

        var response = await host.PostAsJsonAsync($"/api/events/{eventId}/invitees", new { email = unknownEmail });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invitation = await db.Invitations
            .SingleOrDefaultAsync(i => i.EventId == eventId && i.InvitedEmail == unknownEmail);
        Assert.NotNull(invitation);
        Assert.Null(invitation!.UserId);
    }

    [Fact]
    public async Task Host_removes_invitee_marks_invitation_removed_and_clears_rsvp()
    {
        var (host, _, _) = await AuthedClientAsync("remove-host");
        var (invitee, _, inviteeEmail) = await AuthedClientAsync("remove-invitee");
        var eventId = await CreateEventAsync(host, inviteeEmail);

        // Invitee RSVPs (we set the rsvp directly via DbContext since BT-022 isn't done yet).
        Guid invitationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var inv = await db.Invitations.SingleAsync(i => i.EventId == eventId && i.InvitedEmail == inviteeEmail);
            invitationId = inv.Id;
            var rsvp = PullUp.Domain.Events.Rsvp.Create(eventId, inv.UserId!.Value,
                PullUp.Domain.Events.RsvpStatus.Going, null, DateTimeOffset.UtcNow);
            db.Rsvps.Add(rsvp);
            await db.SaveChangesAsync();
        }

        var response = await host.DeleteAsync($"/api/events/{eventId}/invitees/{invitationId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var inv = await db.Invitations.SingleAsync(i => i.Id == invitationId);
            Assert.NotNull(inv.RemovedAt);

            var rsvp = await db.Rsvps.SingleOrDefaultAsync(r => r.EventId == eventId && r.UserId == inv.UserId);
            Assert.Null(rsvp);
        }
    }

    [Fact]
    public async Task Non_host_attempts_to_add_invitee_returns_403()
    {
        var (host, _, _) = await AuthedClientAsync("nonhost-add-host");
        var (stranger, _, _) = await AuthedClientAsync("nonhost-add-stranger");
        var eventId = await CreateEventAsync(host);

        var response = await stranger.PostAsJsonAsync($"/api/events/{eventId}/invitees", new
        {
            email = $"hijacked.{Guid.NewGuid():N}@example.com",
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
