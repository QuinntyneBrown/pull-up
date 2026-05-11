using Microsoft.EntityFrameworkCore;
using PullUp.Domain.Audit;
using PullUp.Domain.Notifications;
using PullUp.Domain.Users;

namespace PullUp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<AuditLogEntry> AuditLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
