namespace PullUp.Application.Abstractions;

// Deferred-integration boundary. The MVP wiring is LoggingEmailSender (no-op
// logger). Production replaces it with a real provider (SES, SendGrid, etc.)
// behind the same interface.
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetTokenRaw,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
