using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PullUp.Api.IntegrationTests.Logging;

// A TestWebApplicationFactory variant that installs a CapturingLoggerProvider so
// tests can assert on log output. Subclassing the existing factory keeps the SQLite
// in-memory wiring intact.
public sealed class RedactingFactory : TestWebApplicationFactory
{
    public CapturingLoggerProvider Capturing { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddProvider(Capturing);
        });
    }
}
