using PullUp.Application.Abstractions;

namespace PullUp.Api.IntegrationTests.Auth;

// Test-only IEmailSender that captures the raw tokens so tests can submit them
// against the corresponding /confirm endpoints. Production wires
// LoggingEmailSender.
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastResetTargetEmail { get; private set; }
    public string? LastResetRawToken { get; private set; }
    public DateTimeOffset? LastResetExpiresAt { get; private set; }

    public string? LastEmailChangeTargetEmail { get; private set; }
    public string? LastEmailChangeRawToken { get; private set; }
    public DateTimeOffset? LastEmailChangeExpiresAt { get; private set; }

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

    public Task SendEmailChangeVerificationAsync(
        string toEmail,
        string verificationTokenRaw,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        LastEmailChangeTargetEmail = toEmail;
        LastEmailChangeRawToken = verificationTokenRaw;
        LastEmailChangeExpiresAt = expiresAt;
        return Task.CompletedTask;
    }
}
