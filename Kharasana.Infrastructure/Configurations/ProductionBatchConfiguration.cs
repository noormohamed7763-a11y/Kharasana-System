using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class ProductionBatchConfiguration : IEntityTypeConfiguration<ProductionBatch>
{
    public void Configure(EntityTypeBuilder<ProductionBatch> builder)
    {
        builder.ToTable("ProductionBatches");

        builder.HasKey(p => p.ProductionBatchId);

        builder.Property(p => p.Quantity)
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ConcreteType)
            .WithMany()
            .HasForeignKey(p => p.ConcreteTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Factory)
            .WithMany()
            .HasForeignKey(p => p.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ Indexes — عزل المصنع + الاستعلام المتكرر
        builder.HasIndex(p => p.FactoryId);
        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => new { p.FactoryId, p.Status })
            .HasDatabaseName("IX_ProductionBatches_FactoryId_Status_Filtered")
            .HasFilter("[IsDeleted] = 0");
    }
}
