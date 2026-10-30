using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.TripId);

        builder.Property(t => t.Quantity)
            .HasPrecision(18, 2);

        // العلاقات
        builder.HasOne(t => t.Order)
            .WithMany(o => o.Trips)
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.PumpDriver)
            .WithMany()
            .HasForeignKey(t => t.PumpDriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Factory)
            .WithMany()
            .HasForeignKey(t => t.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
