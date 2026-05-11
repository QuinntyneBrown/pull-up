using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Events;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.HostId).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.StartsAtUtc).IsRequired();
        builder.Property(e => e.EndsAtUtc);
        builder.Property(e => e.AllowPlusOne).IsRequired();
        builder.Property(e => e.ShowGuestList).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.HostId);
        builder.HasIndex(e => e.StartsAtUtc);
    }
}
