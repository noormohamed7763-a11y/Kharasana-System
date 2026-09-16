using FluentValidation;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Auth;

public class RegisterClientValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterClientValidator()
    {
        RuleFor(x => x.FullName).FullName();

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح.");

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.Password).WithMessage("كلمتا المرور غير متطابقتين.");

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();
    }
}