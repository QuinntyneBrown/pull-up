using Microsoft.EntityFrameworkCore;
using PullUp.Application.Abstractions;
using PullUp.Domain.Audit;
using PullUp.Domain.Events;
using PullUp.Domain.Notifications;
using PullUp.Domain.Users;

namespace PullUp.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
