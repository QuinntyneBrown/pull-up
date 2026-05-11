// Acceptance Test
// Traces to: L2-023 (event detail content), L2-027 (non-host/invitee returns 403),
// L2-036 (aggregate RSVP counts on detail).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Events.GetEvent;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class GetEventTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GetEventTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, Guid userId, string email)> AuthedClientAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var email = $"{emailPrefix}.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await client.PostAsJsonAsync("/api/users", new { fullName = $"{emailPrefix} User", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.UserId, email);
    }

    [Fact]
    public async Task Host_gets_full_detail_with_isHost_and_RSVP_counts()
    {
        var (host, hostId, _) = await AuthedClientAsync("host");
        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Detail Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "Bring snacks",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.NotNull(createBody);

        var detail = await host.GetFromJsonAsync<GetEventResponse>($"/api/events/{createBody!.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Detail Test", detail!.Title);
        Assert.Equal("Home", detail.Location);
        Assert.True(detail.IsHost);
        Assert.Equal(hostId, detail.Host.UserId);
        Assert.Equal("Going", detail.MyRsvpStatus);
        Assert.Equal(1, detail.GoingCount); // host self-RSVP
        Assert.Equal(0, detail.MaybeCount);
        Assert.Equal(0, detail.CantGoCount);
    }

    [Fact]
    public async Task Invitee_sees_detail_with_isHost_false()
    {
        var (host, _, _) = await AuthedClientAsync("host2");
        var (invitee, _, inviteeEmail) = await AuthedClientAsync("invitee");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Invited Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = new[] { inviteeEmail },
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        var detail = await invitee.GetFromJsonAsync<GetEventResponse>($"/api/events/{createBody!.Id}");
        Assert.NotNull(detail);
        Assert.False(detail!.IsHost);
        Assert.Null(detail.MyRsvpStatus);
    }

    [Fact]
    public async Task Non_invitee_returns_403()
    {
        var (host, _, _) = await AuthedClientAsync("host3");
        var (stranger, _, _) = await AuthedClientAsync("stranger");

        var create = await host.PostAsJsonAsync("/api/events", new
        {
            title = "Forbidden Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        var createBody = await create.Content.ReadFromJsonAsync<CreateEventResponse>();

        var response = await stranger.GetAsync($"/api/events/{createBody!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_event_id_returns_404()
    {
        var (host, _, _) = await AuthedClientAsync("host4");
        var response = await host.GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
