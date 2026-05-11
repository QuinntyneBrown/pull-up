using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Notifications;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.NewInvitations).IsRequired();
        builder.Property(p => p.EventReminders).IsRequired();
        builder.Property(p => p.RsvpChanges).IsRequired();
    }
}
