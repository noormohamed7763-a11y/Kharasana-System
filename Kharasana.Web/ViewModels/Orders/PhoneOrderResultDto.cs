using System.Text.Json.Serialization;

namespace Kharasana.Web.ViewModels.Orders;

/// <summary>
/// نتيجة إنشاء طلب هاتفي كما يعيدها الـ API.
/// إن أُنشئ حساب عميل جديد تحمل الاستجابة كلمة مرور مؤقتة تُعرض للموظف
/// <b>مرة واحدة فقط</b> لتسليمها للعميل — لا تُخزَّن نصاً صريحاً في قاعدة البيانات
/// ولا تُعاد في أي استجابة لاحقة.
/// </summary>
public class PhoneOrderResultDto
{
    [JsonPropertyName("order")]
    public OrderDto? Order { get; set; }

    /// <summary>كلمة المرور المؤقتة — null إن كان العميل مسجَّلاً مسبقاً.</summary>
    [JsonPropertyName("newClientTemporaryPassword")]
    public string? NewClientTemporaryPassword { get; set; }

    /// <summary>رقم هاتف العميل الجديد (مُطبَّع) — يُعرض مع كلمة المرور.</summary>
    [JsonPropertyName("newClientPhone")]
    public string? NewClientPhone { get; set; }
}
