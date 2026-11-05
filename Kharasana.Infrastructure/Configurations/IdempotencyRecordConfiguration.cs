using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(i => i.Id);

        // ✅ Index فريد يضمن عدم إمكانية استخدام نفس المفتاح لنفس المستخدم
        //    يجمع بين المفتاح والمستخدم لضمان العزل.
        builder.HasIndex(i => new { i.UserId, i.Key })
            .IsUnique();

        builder.Property(i => i.Key)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(i => i.OperationType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
