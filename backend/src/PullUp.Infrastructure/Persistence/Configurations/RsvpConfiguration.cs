using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Events;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class RsvpConfiguration : IEntityTypeConfiguration<Rsvp>
{
    public void Configure(EntityTypeBuilder<Rsvp> builder)
    {
        builder.ToTable("Rsvps");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.EventId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.Note).HasMaxLength(500);
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
    }
}
