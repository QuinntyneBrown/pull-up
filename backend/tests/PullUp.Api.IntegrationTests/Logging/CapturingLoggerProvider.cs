using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PullUp.Api.IntegrationTests.Logging;

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<CapturingLogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries, categoryName);

    public void Dispose() { }
}
