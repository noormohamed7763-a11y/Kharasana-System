namespace Kharasana.Domain.Enums;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// حالة عمل السائق.
/// <para><c>Unset = 0</c> قيمة حارسة تعني «لم تُمرَّر الحالة» في الـ DTO — يسقطها
/// <c>UserService</c> إلى <see cref="Offline"/> — وليست حالة سائق فعلية، فلا تُعرض
/// في أي قائمة اختيار. الاسم العربي مصدره الوحيد <c>[Display]</c> هنا (كما في
/// <see cref="UserRole"/> و<see cref="TransportMethod"/>)، ولا يؤثر في <c>ToString()</c>.</para>
/// </summary>
public enum DriverStatus
{
    /// <summary>لم تُحدَّد الحالة — قيمة حارسة لا تُعرض ولا تُحفظ صراحةً.</summary>
    [Display(Name = "غير محدّد")]
    Unset = 0,

    [Display(Name = "متاح")]
    Available = 1,

    [Display(Name = "مشغول")]
    Busy = 2,

    [Display(Name = "غير متصل")]
    Offline = 3
}
