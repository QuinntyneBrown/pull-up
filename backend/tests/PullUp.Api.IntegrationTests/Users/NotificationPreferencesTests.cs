// Acceptance Test
// Traces to: L2-016 (toggle individual notification settings),
// L2-017 (defaults are all-on at registration).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PullUp.Application.Features.Users.GetNotificationPreferences;
using PullUp.Application.Features.Users.SignInUser;
using Xunit;

namespace PullUp.Api.IntegrationTests.Users;

public sealed class NotificationPreferencesTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public NotificationPreferencesTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"notif.{Guid.NewGuid():N}@example.com";
        const string password = "Hunter2!secret";
        await client.PostAsJsonAsync("/api/users", new { fullName = "Notif Test", email, password });
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });
        var body = await signIn.Content.ReadFromJsonAsync<SignInUserResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Fresh_user_has_all_three_toggles_on_by_default()
    {
        var client = await AuthedClientAsync();

        var prefs = await client.GetFromJsonAsync<NotificationPreferencesResponse>("/api/users/me/notification-preferences");
        Assert.NotNull(prefs);
        Assert.True(prefs!.NewInvitations);
        Assert.True(prefs.EventReminders);
        Assert.True(prefs.RsvpChanges);
    }

    [Fact]
    public async Task Toggling_a_setting_persists_and_returns_the_latest_state()
    {
        var client = await AuthedClientAsync();

        var update = await client.PutAsJsonAsync("/api/users/me/notification-preferences", new
        {
            newInvitations = false,
            eventReminders = true,
            rsvpChanges = false,
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var prefs = await client.GetFromJsonAsync<NotificationPreferencesResponse>("/api/users/me/notification-preferences");
        Assert.NotNull(prefs);
        Assert.False(prefs!.NewInvitations);
        Assert.True(prefs.EventReminders);
        Assert.False(prefs.RsvpChanges);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/users/me/notification-preferences");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
