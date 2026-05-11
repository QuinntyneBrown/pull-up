namespace PullUp.Api.Logging;

// Logs every incoming request with method, path, and a *redacted* JSON body for
// application/json POST/PUT/PATCH/DELETE. The default ASP.NET Core HTTP-logging
// middleware does not redact, so we own this to honor L2-044 / L2-050.
public sealed class RequestLoggingMiddleware
{
    private static readonly HashSet<string> BodyMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE",
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? bodyForLog = null;

        if (BodyMethods.Contains(context.Request.Method)
            && (context.Request.ContentLength ?? 0) > 0
            && (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(
                context.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true))
            {
                var raw = await reader.ReadToEndAsync(context.RequestAborted);
                bodyForLog = JsonBodyRedactor.Redact(raw);
            }
            context.Request.Body.Position = 0;
        }

        if (bodyForLog is not null)
        {
            _logger.LogInformation(
                "HTTP {Method} {Path} body: {Body}",
                context.Request.Method,
                context.Request.Path,
                bodyForLog);
        }
        else
        {
            _logger.LogInformation(
                "HTTP {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        await _next(context);
    }
}
