using System.Text.Json.Serialization;

namespace Kharasana.Domain.Enums;

/// <summary>
/// حالات الطلب في النظام
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    /// <summary>
    /// <b>قيمة تاريخية لا يُسنِدها النظام.</b> الطلبات تُنشأ بـ <see cref="Pending"/>
    /// منذ أن صار الإنشاء يمرّ بمراجعة موظف المصنع (انظر <c>OrderService.Commands</c>).
    ///
    /// <para>سبب بقائها: قيمتها <c>0</c> وهي الافتراضية في CLR، فقد تحملها صفوف قديمة
    /// في قاعدة البيانات. ولهذا تعاملها كل مواضع المقارنة كـ <see cref="Pending"/>
    /// (<c>status == New || status == Pending</c>)، ويمنحها <see cref="OrderStatusHelper"/>
    /// انتقالات <see cref="Pending"/> نفسها بدل تركها بلا انتقالات.</para>
    ///
    /// <para><b>لا تُحذف</b> قبل migration يعيد إسناد الصفوف القديمة إلى <c>Pending</c>
    /// وإلا قُئرت تلك الصفوف بقيمة لا وجود لها.</para>
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