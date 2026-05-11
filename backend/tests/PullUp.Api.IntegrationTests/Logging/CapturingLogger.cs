using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PullUp.Api.IntegrationTests.Logging;

public sealed class CapturingLogger : ILogger
{
    private readonly ConcurrentBag<CapturingLogEntry> _entries;
    private readonly string _category;

    public CapturingLogger(ConcurrentBag<CapturingLogEntry> entries, string category)
    {
        _entries = entries;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new CapturingLogEntry(logLevel, _category, formatter(state, exception)));
    }
}
