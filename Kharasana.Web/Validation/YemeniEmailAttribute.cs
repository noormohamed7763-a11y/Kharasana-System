using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.Validation;

/// <summary>
/// سمة تحقق من صحة البريد الإلكتروني لـ ViewModels الخاصة بنماذج الويب (Razor).
/// القيمة الفارغة ("" أو مسافات) تُعتبر صالحة لأن الحقل اختياري في النماذج.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class YemeniEmailAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    public override bool IsValid(object? value)
    {
        // null أو سلسلة فارغة/مسافات = صالحة (الحقل اختياري)
        if (value is null)
            return true;

        if (value is not string str)
            return false;

        if (string.IsNullOrWhiteSpace(str))
            return true;

        return Inner.IsValid(str);
    }
}
