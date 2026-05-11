using Microsoft.Extensions.Logging;

namespace PullUp.Api.IntegrationTests.Logging;

public sealed record CapturingLogEntry(LogLevel Level, string Category, string Message);
