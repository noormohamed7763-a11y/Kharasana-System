using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter("[Phone] IS NOT NULL");

        builder.Property(u => u.WhatsApp)
            .HasMaxLength(20);

        builder.Property(u => u.ProfileImage)
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .HasConversion<int>();

        builder.Property(u => u.LicenseNumber)
            .HasMaxLength(50);

        builder.Property(u => u.DriverStatus)
            .HasConversion<int?>();

        builder.HasOne(u => u.Factory)
            .WithMany(f => f.Users)
            .HasForeignKey(u => u.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ClientOrders)
            .WithOne(o => o.Client)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.DriverOrders)
            .WithOne(o => o.Driver)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ RowVersion — ConcurrentUpdate
        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        // ✅ Indexes — أعمدة الاستعلام المتكررة
        builder.HasIndex(u => u.FactoryId);
        builder.HasIndex(u => u.Role);
        builder.HasIndex(u => new { u.Role, u.FactoryId });

        // ✅ حقول الحماية من التخمين السريع (brute-force)
        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);
    }
}