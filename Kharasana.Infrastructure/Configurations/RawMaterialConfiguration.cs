using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterial>
{
    public void Configure(EntityTypeBuilder<RawMaterial> builder)
    {
        builder.ToTable("RawMaterials");

        builder.HasKey(r => r.RawMaterialId);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.CurrentStock)
            .HasPrecision(18, 3); // دقة أكبر للمواد الخام

        builder.Property(r => r.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.HasIndex(r => r.FactoryId);
    }
}
