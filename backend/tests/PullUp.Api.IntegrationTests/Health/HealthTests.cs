// Acceptance Test
// Traces to: L2-064 (health-check endpoints).
// Description: /health/live always returns 200 while the process is alive;
// /health/ready returns 200 when the DB connection succeeds.

using System.Net;
using Xunit;

namespace PullUp.Api.IntegrationTests.Health;

public sealed class HealthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public HealthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Live_endpoint_returns_200_with_healthy_status()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\"", body);
        Assert.Contains("healthy", body);
    }

    [Fact]
    public async Task Ready_endpoint_returns_200_when_database_is_reachable()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ready", body);
    }
}
