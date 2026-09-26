namespace Kharasana.Application.DTOs.Order;

/// <summary>
/// نتيجة إنشاء طلب هاتفي.
/// تحمل تفاصيل الطلب، ومعها كلمة المرور المؤقتة للحساب الجديد إن أُنشئ حساب.
/// كلمة المرور تُعاد <b>مرة واحدة فقط</b> هنا ولا تُخزَّن نصاً صريحاً في قاعدة البيانات
/// (يُخزَّن تجزئتها فقط) ولا تُعاد في أي استعلام لاحق.
/// </summary>
public class PhoneOrderResultDto
{
    public required OrderDetailsDto Order { get; init; }

    /// <summary>كلمة المرور المؤقتة — null إن كان العميل مسجَّلاً مسبقاً فلم تُنشأ كلمة مرور.</summary>
    public string? NewClientTemporaryPassword { get; init; }

    /// <summary>رقم هاتف العميل الجديد (مُطبَّع) — يُعرض مع كلمة المرور لتسليمها للعميل.</summary>
    public string? NewClientPhone { get; init; }
}
