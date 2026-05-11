// Acceptance Test
// Traces to: L2-026 (host edits event), L2-027 (non-host -> 403),
// L2-028 (date/time/location changes notify invitees; description-only does not).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class UpdateEventTests : IClassFixture<NotificationCapturingFactory>
{
    private readonly NotificationCapturingFactory _factory;

    public UpdateEventTests(NotificationCapturingFactory factory)
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
    public async Task Host_can_update_event_and_changes_are_reflected_on_detail()
    {
        var (host, _, _) = await AuthedClientAsync("update-host");
        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Original Title",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Original Location",
            description = "Original",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        var newDate = DateTimeOffset.UtcNow.AddDays(10);
        var update = await host.PutAsJsonAsync($"/api/events/{createBody!.Id}", new
        {
            title = "Updated Title",
            startsAtUtc = newDate,
            location = "Updated Location",
            description = "Updated",
            allowPlusOne = false,
            showGuestList = false,
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var detail = await host.GetFromJsonAsync<GetEventResponse>($"/api/events/{createBody.Id}");
        Assert.Equal("Updated Title", detail!.Title);
        Assert.Equal("Updated Location", detail.Location);
        Assert.Equal("Updated", detail.Description);
        Assert.False(detail.AllowPlusOne);
        Assert.False(detail.ShowGuestList);
    }

    [Fact]
    public async Task Updating_date_or_location_fans_out_EventUpdated_notifications()
    {
        var (host, _, _) = await AuthedClientAsync("update-notify-host");
        var (_, inviteeId, inviteeEmail) = await AuthedClientAsync("update-notify-invitee");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Notify Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Original",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        _factory.Notifications.Sent.Clear();

        var update = await host.PutAsJsonAsync($"/api/events/{createBody!.Id}", new
        {
            title = "Notify Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),  // changed
            location = "New Location",                       // changed
            description = "",
            allowPlusOne = true,
            showGuestList = true,
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        Assert.Contains(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == inviteeId
                 && n.Kind == NotificationKind.EventUpdated
                 && n.EventId == createBody.Id);
    }

    [Fact]
    public async Task Description_only_change_does_not_dispatch_notifications()
    {
        var (host, _, _) = await AuthedClientAsync("update-desc-host");
        var (_, _, inviteeEmail) = await AuthedClientAsync("update-desc-invitee");

        var startsAt = DateTimeOffset.UtcNow.AddDays(5);
        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Desc Only",
            startsAtUtc = startsAt,
            location = "Same",
            description = "Original",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        _factory.Notifications.Sent.Clear();

        var update = await host.PutAsJsonAsync($"/api/events/{createBody!.Id}", new
        {
            title = "Desc Only",
            startsAtUtc = startsAt,
            location = "Same",
            description = "Brand new description",  // only this changes
            allowPlusOne = true,
            showGuestList = true,
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        Assert.DoesNotContain(_factory.Notifications.Sent, n => n.Kind == NotificationKind.EventUpdated);
    }

    [Fact]
    public async Task Non_host_cannot_update_returns_403()
    {
        var (host, _, _) = await AuthedClientAsync("update-host3");
        var (stranger, _, _) = await AuthedClientAsync("update-stranger");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Forbidden Update",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        var response = await stranger.PutAsJsonAsync($"/api/events/{createBody!.Id}", new
        {
            title = "Hijacked",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(10),
            location = "Other",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
