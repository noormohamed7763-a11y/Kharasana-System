using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Validation;

/// <summary>
/// سمة تحقق مركزيّة من البريد الإلكتروني.
///
/// على عكس <see cref="EmailAddressAttribute"/> المدمجة، فإن القيمة الفارغة ("" أو مسافات)
/// تُعتبر <b>صالحة</b> لأن الحقل اختياري — بينما <see cref="EmailAddressAttribute.IsValid(string)"/>
/// ترفض السلاسل الفارغة وتسبّب فشل النموذج حتى عندما يُترك الحقل فارغاً.
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
