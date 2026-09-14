using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Common;

namespace Kharasana.Domain.Validation;

/// <summary>
/// تحقق من صحة رقم هاتف يمني على خصائص ViewModels / DTOs (DataAnnotations).
/// قاعدة التحقق الموحّدة هي YemeniPhoneHelper في Domain.Common — لا تُكتب قواعد هنا.
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