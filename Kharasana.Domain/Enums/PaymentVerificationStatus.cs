namespace Kharasana.Domain.Enums;

/// <summary>
/// حالة التحقق من الدفع/الحوالة من قبل المحاسب
/// </summary>
public enum PaymentVerificationStatus
{
    /// <summary>
    /// الدفعة وصلت كنقد (كاش) أو إيداع بنكي مباشر - لا تحتاج تحقق
    /// </summary>
    NotApplicable = 0,

    /// <summary>
    /// تم استلام رقم الحوالة من العميل/السائق، وبانتظار مراجعة المحاسب
    /// </summary>
    PendingVerification = 1,

    /// <summary>
    /// المحاسب تحقق من وصول الحوالة وقبولها
    /// </summary>
    Verified = 2,

    /// <summary>
    /// المحاسب رفض الحوالة (رقم خاطئ، لم تصل، مبلغ مختلف)
    /// </summary>
    Rejected = 3
}