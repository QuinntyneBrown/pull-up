using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PullUp.Domain.Users;

namespace PullUp.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128)
            .IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => new { t.UserId, t.RevokedAt });

        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.RevokedAt);
        builder.Property(t => t.ReplacedByTokenId);
    }
}
