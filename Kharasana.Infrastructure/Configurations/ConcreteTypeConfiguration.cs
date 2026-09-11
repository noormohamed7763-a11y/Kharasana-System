using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class ConcreteTypeConfiguration : IEntityTypeConfiguration<ConcreteType>
{
    public void Configure(EntityTypeBuilder<ConcreteType> builder)
    {
        builder.ToTable("ConcreteTypes");

        builder.HasKey(c => c.ConcreteTypeId);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.HasIndex(c => new { c.FactoryId, c.Name })
            .IsUnique();

        builder.HasOne(c => c.Factory)
            .WithMany(f => f.ConcreteTypes)
            .HasForeignKey(c => c.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Orders)
            .WithOne(o => o.ConcreteType)
            .HasForeignKey(o => o.ConcreteTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}