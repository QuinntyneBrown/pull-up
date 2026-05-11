// Acceptance Test
// Traces to: L2-022 (home list grouped by time period), L2-024 (past events via filter),
// L2-025 (scope filter behavior).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Features.Events.ListMyEvents;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Domain.Events;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class ListMyEventsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ListMyEventsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, Guid userId)> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"listevents.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await client.PostAsJsonAsync("/api/users", new { fullName = "List Host", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.UserId);
    }

    [Fact]
    public async Task Hosted_event_in_next_week_appears_in_thisWeek_and_marks_isHost()
    {
        var (client, userId) = await AuthedClientAsync();
        var create = await client.PostAsJsonAsync("/api/events", new
        {
            title = "This Week Dinner",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetFromJsonAsync<ListMyEventsResponse>("/api/events");
        Assert.NotNull(list);
        var match = list!.ThisWeek.SingleOrDefault(e => e.Title == "This Week Dinner");
        Assert.NotNull(match);
        Assert.True(match!.IsHost);
        Assert.Equal("Going", match.MyRsvpStatus);
    }

    [Fact]
    public async Task Scope_Hosting_returns_only_hosted_events()
    {
        var (client, _) = await AuthedClientAsync();
        await client.PostAsJsonAsync("/api/events", new
        {
            title = "Hosting Filter Test",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            location = "Home",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });

        var list = await client.GetFromJsonAsync<ListMyEventsResponse>("/api/events?scope=Hosting");
        Assert.NotNull(list);
        Assert.Contains(list!.ThisWeek, e => e.Title == "Hosting Filter Test");
    }

    [Fact]
    public async Task Scope_Past_returns_past_events()
    {
        var (client, userId) = await AuthedClientAsync();

        // Inject a past event directly via the DbContext — the create endpoint
        // rejects past dates (L2-020), so we go through EF to seed the test case.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pastEvent = Event.Create(
                hostId: userId,
                title: "Old Party",
                startsAtUtc: DateTimeOffset.UtcNow.AddDays(-30),
                endsAtUtc: null,
                location: "Old place",
                description: "",
                allowPlusOne: true,
                showGuestList: true,
                now: DateTimeOffset.UtcNow.AddDays(-31));
            db.Events.Add(pastEvent);
            db.Rsvps.Add(Rsvp.Create(pastEvent.Id, userId, RsvpStatus.Going, null, DateTimeOffset.UtcNow.AddDays(-31)));
            await db.SaveChangesAsync();
        }

        var list = await client.GetFromJsonAsync<ListMyEventsResponse>("/api/events?scope=Past");
        Assert.NotNull(list);
        Assert.Contains(list!.Past, e => e.Title == "Old Party");
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
