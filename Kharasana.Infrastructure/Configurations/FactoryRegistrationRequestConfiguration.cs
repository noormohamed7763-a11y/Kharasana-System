using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Infrastructure.Configurations;

public class FactoryRegistrationRequestConfiguration : IEntityTypeConfiguration<FactoryRegistrationRequest>
{
    public void Configure(EntityTypeBuilder<FactoryRegistrationRequest> builder)
    {
        builder.ToTable("FactoryRegistrationRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.FactoryName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Area).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Address).IsRequired().HasMaxLength(500);
        builder.Property(r => r.CommercialId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.ContactName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(r => r.ContactPhone).IsRequired().HasMaxLength(20);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(RegistrationStatus.Pending);

        builder.Property(r => r.RejectionReason).HasMaxLength(500);

        builder.HasOne(r => r.Factory)
            .WithOne()
            .HasForeignKey<FactoryRegistrationRequest>(r => r.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.FactoryId)
            .IsUnique()
            .HasFilter("[FactoryId] IS NOT NULL");

        builder.HasOne(r => r.ProcessedBy)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
