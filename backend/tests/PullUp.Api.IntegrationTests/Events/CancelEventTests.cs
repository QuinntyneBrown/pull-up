// Acceptance Test
// Traces to: L2-029 (host can cancel; status flips), L2-030 (notification dispatched
// to invitees on cancellation), L2-027 (non-host -> 403), L2-037 (dispatch infra).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Abstractions;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class CancelEventTests : IClassFixture<NotificationCapturingFactory>
{
    private readonly NotificationCapturingFactory _factory;

    public CancelEventTests(NotificationCapturingFactory factory)
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
    public async Task Host_can_cancel_event_and_invitees_receive_EventCancelled_notification()
    {
        var (host, _, _) = await AuthedClientAsync("cancel-host");
        var (_, inviteeId, inviteeEmail) = await AuthedClientAsync("cancel-invitee");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Cancel Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.NotNull(createBody);

        _factory.Notifications.Sent.Clear();

        var cancel = await host.PostAsync($"/api/events/{createBody!.Id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var detail = await host.GetFromJsonAsync<GetEventResponse>($"/api/events/{createBody.Id}");
        Assert.Equal("Cancelled", detail!.Status);

        Assert.Contains(
            _factory.Notifications.Sent,
            n => n.RecipientUserId == inviteeId
                 && n.Kind == NotificationKind.EventCancelled
                 && n.EventId == createBody.Id);
    }

    [Fact]
    public async Task Non_host_cannot_cancel_returns_403()
    {
        var (host, _, _) = await AuthedClientAsync("cancel-host2");
        var (stranger, _, _) = await AuthedClientAsync("cancel-stranger");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Forbidden Cancel",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        var response = await stranger.PostAsync($"/api/events/{createBody!.Id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_event_returns_404()
    {
        var (host, _, _) = await AuthedClientAsync("cancel-host3");
        var response = await host.PostAsync($"/api/events/{Guid.NewGuid()}/cancel", content: null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
