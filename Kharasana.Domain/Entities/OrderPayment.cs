using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class OrderPayment
{
    public int OrderPaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? ReferenceNumber { get; set; } // رقم الحوالة أو الإيصال

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PaymentVerificationStatus VerificationStatus { get; set; } = PaymentVerificationStatus.PendingVerification;

    public string? VerificationNotes { get; set; }

    public DateTime? VerifiedAt { get; set; }

    // Navigation property
    public Order Order { get; set; } = null!;
}