using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Common;

namespace Kharasana.Web.Validation;

/// <summary>
/// سمة تحقق من صحة رقم الهاتف اليمني لـ ViewModels الخاصة بنماذج الويب (Razor).
/// تعتمد حصرياً على المنطق النقي في <see cref="YemeniPhoneHelper"/> كمصدر وحيد للحقيقة.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class YemeniPhoneAttribute : ValidationAttribute
{
    public const string DefaultErrorMessage = "رقم الهاتف غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).";

    public YemeniPhoneAttribute()
    {
        ErrorMessage = DefaultErrorMessage;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // الحقل اختياري: null أو فارغ = صالح (لا يوجد إدخال)
        if (value is not string raw || string.IsNullOrWhiteSpace(raw))
            return ValidationResult.Success;

        return YemeniPhoneHelper.IsValid(raw)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? DefaultErrorMessage);
    }
}
