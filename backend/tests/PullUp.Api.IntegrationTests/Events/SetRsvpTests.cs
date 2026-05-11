// Acceptance Test
// Traces to: L2-034 (invitee can set RSVP), L2-035 (change RSVP before event date,
// past event -> 409), L2-036 (aggregate counts), L2-039 (RSVP change notifies host).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Domain.Events;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class SetRsvpTests : IClassFixture<NotificationCapturingFactory>
{
    private readonly NotificationCapturingFactory _factory;

    public SetRsvpTests(NotificationCapturingFactory factory)
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

    [Fact]
    public async Task Invitee_can_set_RSVP_and_host_gets_RsvpChanged_notification()
    {
        var (host, hostId, _) = await AuthedClientAsync("rsvp-host");
        var (invitee, _, inviteeEmail) = await AuthedClientAsync("rsvp-invitee");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "RSVP Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var eventId = (await create.Content.ReadFromJsonAsync<CreateEventResponse>())!.Id;

        _factory.Notifications.Sent.Clear();

        var response = await invitee.PutAsJsonAsync($"/api/events/{eventId}/rsvp", new
        {
            status = "Going",
            note = "bringing flan",
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await host.GetFromJsonAsync<GetEventResponse>($"/api/events/{eventId}");
        Assert.Equal(2, detail!.GoingCount); // host + invitee

        Assert.Contains(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == hostId
                 && n.Kind == NotificationKind.RsvpChanged
                 && n.EventId == eventId);
    }

    [Fact]
    public async Task Invitee_changing_RSVP_updates_existing_row()
    {
        var (host, _, _) = await AuthedClientAsync("rsvp-change-host");
        var (invitee, inviteeId, inviteeEmail) = await AuthedClientAsync("rsvp-change-invitee");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "RSVP Change",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var eventId = (await create.Content.ReadFromJsonAsync<CreateEventResponse>())!.Id;

        await invitee.PutAsJsonAsync($"/api/events/{eventId}/rsvp", new { status = "Going", note = (string?)null });
        await invitee.PutAsJsonAsync($"/api/events/{eventId}/rsvp", new { status = "Maybe", note = "tentative" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rsvps = await db.Rsvps.Where(r => r.EventId == eventId && r.UserId == inviteeId).ToListAsync();
        Assert.Single(rsvps);
        Assert.Equal(RsvpStatus.Maybe, rsvps[0].Status);
        Assert.Equal("tentative", rsvps[0].Note);
    }

    [Fact]
    public async Task Past_event_returns_409_with_EVENT_PASSED()
    {
        var (host, hostId, _) = await AuthedClientAsync("rsvp-past-host");
        var (invitee, inviteeId, inviteeEmail) = await AuthedClientAsync("rsvp-past-invitee");

        // Seed a past event + invitation directly so we can test the past-event branch
        // (create endpoint rejects past dates per L2-020).
        Guid eventId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pastEvent = Event.Create(
                hostId: hostId,
                title: "Past RSVP Test",
                startsAtUtc: DateTimeOffset.UtcNow.AddDays(-30),
                endsAtUtc: null,
                location: "Old place",
                description: "",
                allowPlusOne: true,
                showGuestList: true,
                now: DateTimeOffset.UtcNow.AddDays(-31));
            db.Events.Add(pastEvent);
            db.Invitations.Add(Invitation.Create(pastEvent.Id, inviteeId, inviteeEmail, DateTimeOffset.UtcNow.AddDays(-31)));
            await db.SaveChangesAsync();
            eventId = pastEvent.Id;
        }

        var response = await invitee.PutAsJsonAsync($"/api/events/{eventId}/rsvp", new
        {
            status = "Going",
            note = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("event has already", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Non_invitee_returns_403()
    {
        var (host, _, _) = await AuthedClientAsync("rsvp-noinv-host");
        var (stranger, _, _) = await AuthedClientAsync("rsvp-stranger");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Stranger",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var eventId = (await create.Content.ReadFromJsonAsync<CreateEventResponse>())!.Id;

        var response = await stranger.PutAsJsonAsync($"/api/events/{eventId}/rsvp", new
        {
            status = "Going",
            note = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
