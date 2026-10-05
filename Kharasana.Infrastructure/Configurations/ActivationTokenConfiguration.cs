using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kharasana.Domain.Entities;

namespace Kharasana.Infrastructure.Configurations;

public class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
{
    public void Configure(EntityTypeBuilder<ActivationToken> builder)
    {
        builder.ToTable("ActivationTokens");

        builder.HasKey(t => t.ActivationTokenId);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(256);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
