using Kharasana.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kharasana.Infrastructure.Configurations;

/// <summary>
/// تكوين جدول الدفعات (<c>OrderPayments</c>) — سجل حركة الدفع لكل طلب.
///
/// <para><b>سقوف الأعمدة النصّية:</b> <c>ReferenceNumber</c> و<c>Notes</c> مُقيَّدة بسقف
/// (<c>256</c> و<code>1000</code>) لا <c>nvarchar(max)</c>. السبب: العمود من نوع
/// <c>nvarchar(max)</c> لا يملك سقفاً يفرضه النظام، فنصّ طويل يمرّ إلى الجدول بلا اعتراض.
/// وسقف <c>Notes</c> مطابق لسقف <c>Orders.Notes</c> فسقف الملاحظات في النظام واحد.</para>
///
/// <para><b>الحذف المتتالي (Cascade):</b> حذف الطلب في النظام حذف ناعم
/// (<c>IsDeleted</c>) فلا يقع الحذف المادي عادةً؛ لكن إن وقع (ترميم يدوي) فحذف الدفعات
/// معه مقصود — الدفعات لا معنى لها بغير طلبها.</para>
/// </summary>
public class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        builder.ToTable("OrderPayments");

        builder.HasKey(p => p.OrderPaymentId);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentDate)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(256);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.VerificationStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.VerificationNotes)
            .HasMaxLength(1000);

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // مسار الوصول الوحيد للدفعات هو عبر الطلب، ففهرس OrderId يخدمه.
        builder.HasIndex(p => p.OrderId);
    }
}
