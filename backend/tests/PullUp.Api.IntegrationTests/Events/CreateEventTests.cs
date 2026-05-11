// Acceptance Test
// Traces to: L2-018 (create event with required fields -> 201),
// L2-019 (no-invitees creates event with host as sole RSVP),
// L2-020 (past date -> 400), L2-021 (length validation -> 400).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Application.Features.Events.CreateEvent;
using PullUp.Application.Features.Users.SignInUser;
using PullUp.Domain.Events;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Events;

public sealed class CreateEventTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CreateEventTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<(HttpClient client, Guid userId)> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"events.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await client.PostAsJsonAsync("/api/users", new { fullName = "Event Host", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.UserId);
    }

    [Fact]
    public async Task Valid_event_returns_201_and_persists_with_host_RSVP_going()
    {
        var (client, userId) = await AuthedClientAsync();
        var startsAt = DateTimeOffset.UtcNow.AddDays(7);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Marquez family dinner",
            startsAtUtc = startsAt,
            location = "Abuela's house",
            description = "Bring a side",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Events.SingleOrDefaultAsync(e => e.Id == body.Id);
        Assert.NotNull(stored);
        Assert.Equal("Marquez family dinner", stored!.Title);
        Assert.Equal(userId, stored.HostId);

        var hostRsvp = await db.Rsvps.SingleOrDefaultAsync(r => r.EventId == body.Id && r.UserId == userId);
        Assert.NotNull(hostRsvp);
        Assert.Equal(RsvpStatus.Going, hostRsvp!.Status);
    }

    [Fact]
    public async Task Past_date_returns_400()
    {
        var (client, _) = await AuthedClientAsync();
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            title = "Past Party",
            startsAtUtc = pastDate,
            location = "Past place",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("today or later", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Over_length_title_returns_400()
    {
        var (client, _) = await AuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            title = new string('A', 121),
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "OK",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/events", new
        {
            title = "Anonymous Party",
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            location = "Nowhere",
            description = "",
            allowPlusOne = true,
            showGuestList = true,
            inviteeEmails = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
