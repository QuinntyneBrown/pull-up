using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Events;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.EventId).IsRequired();
        builder.Property(i => i.UserId);
        builder.Property(i => i.InvitedEmail).HasMaxLength(254).IsRequired();
        builder.Property(i => i.InvitedAt).IsRequired();
        builder.Property(i => i.RemovedAt);

        builder.HasIndex(i => i.EventId);
        builder.HasIndex(i => i.UserId);
    }
}
