using PullUp.Application.Abstractions;

namespace PullUp.Api.IntegrationTests.Auth;

// Test-only IEmailSender that captures the raw reset token so the test can
// submit /api/auth/password-reset/confirm. Production wires LoggingEmailSender.
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastResetTargetEmail { get; private set; }
    public string? LastResetRawToken { get; private set; }
    public DateTimeOffset? LastResetExpiresAt { get; private set; }

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetTokenRaw,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        LastResetTargetEmail = toEmail;
        LastResetRawToken = resetTokenRaw;
        LastResetExpiresAt = expiresAt;
        return Task.CompletedTask;
    }
}
