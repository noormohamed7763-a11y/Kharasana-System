using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;

namespace Kharasana.Application.Validators.Auth;

public class RegisterClientValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterClientValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(200).WithMessage(Messages.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(6).WithMessage(Messages.PasswordMinLength);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.Password).WithMessage("كلمتا المرور غير متطابقتين.");

        RuleFor(x => x.Phone)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.WhatsApp)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp))
            .WithMessage(Messages.InvalidWhatsAppNumber);
    }
}