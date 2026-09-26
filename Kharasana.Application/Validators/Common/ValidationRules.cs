using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Domain.Common;
using Kharasana.Domain.Validation;

namespace Kharasana.Application.Validators.Common;

/// <summary>
/// قواعد تحقق مشتركة متعددة الاستخدام — توحّد الرسائل والسلوك عبر الفاحصين وتفادي التكرار.
/// </summary>
public static class ValidationRules
{
    /// <summary>الاسم الكامل: مطلوب ولا يتجاوز 200 حرف.</summary>
    public static IRuleBuilderOptions<T, string?> FullName<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .NotEmpty().WithMessage(Messages.FullNameRequired)
            .MaximumLength(200).WithMessage(Messages.NameMaxLength);

    /// <summary>كلمة المرور: مطلوبة وبالحد الأدنى الموحّد للطول (<see cref="PasswordPolicy.MinimumLength"/>).</summary>
    public static IRuleBuilderOptions<T, string?> Password<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .NotEmpty().WithMessage(Messages.PasswordRequired)
            .MinimumLength(PasswordPolicy.MinimumLength).WithMessage(Messages.PasswordMinLength);

    /// <summary>رقم هاتف يمني اختياري — يُقبل الفارغ.</summary>
    public static IRuleBuilderOptions<T, string?> YemeniPhone<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .Must(phone => string.IsNullOrWhiteSpace(phone) || YemeniPhoneHelper.IsValid(phone!))
            .WithMessage(Messages.InvalidYemeniPhone);

    /// <summary>رقم واتساب يمني اختياري — يُقبل الفارغ.</summary>
    public static IRuleBuilderOptions<T, string?> YemeniWhatsApp<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .Must(whatsApp => string.IsNullOrWhiteSpace(whatsApp) || YemeniPhoneHelper.IsValid(whatsApp!))
            .WithMessage(Messages.InvalidWhatsAppNumber);
}