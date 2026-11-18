using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;

namespace Kharasana.Application.Validators.Auth;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.EmailOrPhone)
            .NotEmpty().WithMessage(Messages.LoginIdentifierRequired);

        // ملاحظة: NotEmpty وحده — لا نستعمل ValidationRules.Password هنا، فإضافة
        // حدّ الطول الأدنى إلى الدخول ترفض كلمات مرور قديمة صحيحة (400 بدل 401).
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(Messages.PasswordRequired);
    }
}