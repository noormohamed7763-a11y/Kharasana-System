namespace Kharasana.Domain.Enums;

/// <summary>
/// الأسماء العربية لطرق الدفع — المصدر الوحيد للتمثيل النصّي لطريقة الدفع.
///
/// <para>يُتبع نمط <see cref="OrderStatusHelper"/>: خريطة واحدة في Domain، والويب
/// يستدعيها ولا يبني خريطة موازية (وإلا انحرفت الخريطة تبعاً لمرور الوقت —
/// إضافة طريقة دفع جديدة تُكتب في موضع وتُنسى في آخر فيظهر السند بطريقتين).</para>
/// </summary>
public static class PaymentMethodHelper
{
    /// <summary>الاسم المجرّد لطريقة الدفع، بلا إيموجي ولا زخرفة.</summary>
    public static string GetArabicName(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقداً",
        PaymentMethod.KareemiTransfer => "حوالة كريمي",
        PaymentMethod.BankDeposit => "إيداع بنكي",
        PaymentMethod.MobileMoney => "حوالة موبايل",
        PaymentMethod.Cheque => "شيك",
        PaymentMethod.Other => "أخرى",
        _ => "غير معروف"
    };
}
