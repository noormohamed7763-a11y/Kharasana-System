using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class FactoryConfiguration : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> builder)
    {
        builder.ToTable("Factories");

        builder.HasKey(f => f.FactoryId);

        builder.Property(f => f.FactoryName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(f => f.FactoryName)
            .IsUnique();

        builder.Property(f => f.OwnerName)
            .HasMaxLength(200);

        builder.Property(f => f.Phone)
            .HasMaxLength(20);

        builder.Property(f => f.WhatsApp)
            .HasMaxLength(20);

        builder.Property(f => f.Email)
            .HasMaxLength(256);

        builder.Property(f => f.Area)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Latitude)
            .HasPrecision(10, 7);

        builder.Property(f => f.Longitude)
            .HasPrecision(10, 7);

        builder.Property(f => f.Logo)
            .HasMaxLength(500);

        builder.HasMany(f => f.Users)
            .WithOne(u => u.Factory)
            .HasForeignKey(u => u.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.ConcreteTypes)
            .WithOne(c => c.Factory)
            .HasForeignKey(c => c.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Orders)
            .WithOne(o => o.Factory)
            .HasForeignKey(o => o.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ RowVersion — ConcurrentUpdate
        builder.Property(f => f.RowVersion)
            .IsRowVersion();

        // ✅ Index — الحذف الناعم: فلترة المنشآت النشطة فقط
        builder.HasIndex(f => f.IsDeleted);
    }
}