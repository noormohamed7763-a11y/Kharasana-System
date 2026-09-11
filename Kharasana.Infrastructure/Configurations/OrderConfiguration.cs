using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.ProjectName)
            .HasMaxLength(200);

        builder.Property(o => o.ProjectOwnerName)
            .HasMaxLength(200);

        builder.Property(o => o.SiteArea)
            .HasMaxLength(100);

        builder.Property(o => o.SiteDescription)
            .HasMaxLength(500);

        builder.Property(o => o.SlabType)
            .HasConversion<int>();
        builder.Property(o => o.TransportMethod)
    .HasConversion<int>();

        builder.Property(o => o.Quantity)
            .HasPrecision(18, 2);

        builder.Property(o => o.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(o => o.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(o => o.TruckPlate)
            .HasMaxLength(30);

        builder.Property(o => o.Status)
            .HasConversion<int>();

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.HasOne(o => o.Client)
            .WithMany(u => u.ClientOrders)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Driver)
            .WithMany(u => u.DriverOrders)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Factory)
            .WithMany(f => f.Orders)
            .HasForeignKey(o => o.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ConcreteType)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.ConcreteTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ RowVersion — ConcurrentUpdate (يمنع التعارض المزدوج)
        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        // ✅ Indexes — أعمدة الاستعلام المتكررة
        builder.HasIndex(o => o.FactoryId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.ClientId);
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => new { o.FactoryId, o.Status });
    }
}