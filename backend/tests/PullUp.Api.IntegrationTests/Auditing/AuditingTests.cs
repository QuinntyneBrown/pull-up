// Acceptance Test
// Traces to: L2-005 (sign-in failure audit), L2-010 (password-reset audit),
// L2-043 (audit on failed sign-in), L2-060 (security-event audit-log content),
// L2-061 (90-day query window).
// Description: AuditingBehavior writes an AuditLogEntry row for any request
// annotated with [AuditedAction("...")], using event = the supplied marker and
// outcome = SUCCESS or FAILURE depending on whether the handler completed normally.
// Exercised end-to-end via the registration slice, which now carries
// [AuditedAction("USER_REGISTERED")].

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Infrastructure.Persistence;
using Xunit;

namespace PullUp.Api.IntegrationTests.Auditing;

public sealed class AuditingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Successful_register_writes_audit_row_with_outcome_SUCCESS()
    {
        var email = $"audit-success.{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Audit Success",
            email,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var row = await db.AuditLog
            .Where(a => a.Event == "USER_REGISTERED" && a.Outcome == "SUCCESS")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(row);
        Assert.True(row!.OccurredAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.NotEqual(Guid.Empty, row.CorrelationId);
    }

    [Fact]
    public async Task Failed_register_writes_audit_row_with_outcome_FAILURE()
    {
        var email = $"audit-fail.{Guid.NewGuid():N}@example.com";

        // First register succeeds — produces a SUCCESS row we will filter out below.
        var first = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Audit Fail Setup",
            email,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second attempt with the same email should 409 and emit a FAILURE row.
        var second = await _client.PostAsJsonAsync("/api/users", new
        {
            fullName = "Audit Fail Duplicate",
            email,
            password = "Hunter2!secret",
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var row = await db.AuditLog
            .Where(a => a.Event == "USER_REGISTERED" && a.Outcome == "FAILURE")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(row);
    }
}
