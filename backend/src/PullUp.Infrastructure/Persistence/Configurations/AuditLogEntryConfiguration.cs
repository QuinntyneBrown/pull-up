using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Audit;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Event)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Outcome)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.CorrelationId).IsRequired();
        builder.Property(e => e.MetadataJson);

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.ActorUserId, e.OccurredAt });
    }
}
