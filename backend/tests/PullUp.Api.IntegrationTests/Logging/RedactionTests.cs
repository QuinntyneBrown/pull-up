// Acceptance Test
// Traces to: L2-044 (secrets and tokens never logged), L2-050 (sensitive data not in logs).
// Description: POST /api/users with a real password; assert no captured log entry contains
// the plaintext password; assert at least one entry contains the redaction marker.

using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PullUp.Api.IntegrationTests.Logging;

public sealed class RedactionTests : IClassFixture<RedactingFactory>
{
    private readonly RedactingFactory _factory;
    private readonly HttpClient _client;

    private const string Plaintext = "Hunter2!secret-no-log-please";

    public RedactionTests(RedactingFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Plaintext_password_never_appears_in_logs_and_redaction_marker_does()
    {
        var email = $"redact.{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Redaction Test",
            email,
            password = Plaintext,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entries = _factory.Capturing.Entries.ToArray();

        Assert.DoesNotContain(entries, e => e.Message.Contains(Plaintext, StringComparison.Ordinal));
        Assert.Contains(entries, e => e.Message.Contains("***REDACTED***", StringComparison.Ordinal));
    }
}
