using System.Text.Json.Serialization;

namespace Kharasana.Domain.Enums;

/// <summary>
/// حالات الطلب في النظام
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    /// <summary>
    /// جديد - تم إنشاء الطلب
    /// </summary>
    New = 0,

    /// <summary>
    /// قيد الانتظار - بانتظار مراجعة موظف المصنع
    /// </summary>
    Pending = 1,

    /// <summary>
    /// معتمد - تمت موافقة العميل والموظف
    /// </summary>
    Approved = 2,

    /// <summary>
    /// مرفوض - المصنع رفض الطلب
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// ملغي - العميل ألغى الطلب
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// في الطريق - تم تعيين سائق وبدأ التوصيل
    /// </summary>
    OnTheWay = 5,

    /// <summary>
    /// تم التسليم - وصل الطلب إلى العميل
    /// </summary>
    Delivered = 6,

    /// <summary>
    /// مغلق - تم إغلاق الطلب نهائياً
    /// </summary>
    Closed = 7
}