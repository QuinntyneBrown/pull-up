using Microsoft.Extensions.Logging;
using PullUp.Application.Abstractions;

namespace PullUp.Infrastructure.Notifications;

// MVP no-op email sender per the BP1 plan §9 "deferred integrations". Logs
// only metadata — the raw reset token is intentionally NOT logged (L2-044).
// Production wiring replaces this with a real SMTP / SES / SendGrid client.
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetTokenRaw,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Password-reset email queued for {Email} (expires {ExpiresAt})",
            toEmail,
            expiresAt);
        return Task.CompletedTask;
    }

    public Task SendEmailChangeVerificationAsync(
        string toEmail,
        string verificationTokenRaw,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email-change verification queued for {Email} (expires {ExpiresAt})",
            toEmail,
            expiresAt);
        return Task.CompletedTask;
    }
}
