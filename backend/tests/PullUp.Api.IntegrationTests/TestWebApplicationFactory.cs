using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PullUp.Infrastructure.Persistence;

namespace PullUp.Api.IntegrationTests;

// Boots the real API pipeline against the SQLite in-memory database described in
// appsettings.Testing.json. The "shared cache" mode means multiple connections to
// the same DataSource name see the same in-memory DB; SQLite drops the DB once the
// last connection closes, so the factory holds one extra connection open for its
// lifetime to keep the DB alive across requests.
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ConnectionString = "DataSource=file:pullup-tests?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public TestWebApplicationFactory()
    {
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        base.ConfigureWebHost(builder);
    }

    private static readonly object _gate = new();
    private static bool _schemaCreated;

    // Both test fixtures resolve to the same in-memory SQLite DB (shared-cache by name),
    // so the schema only needs to be created once per test process.
    public void EnsureDatabaseCreated()
    {
        lock (_gate)
        {
            if (_schemaCreated)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            _schemaCreated = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keepAlive.Dispose();
        }
        base.Dispose(disposing);
    }
}
