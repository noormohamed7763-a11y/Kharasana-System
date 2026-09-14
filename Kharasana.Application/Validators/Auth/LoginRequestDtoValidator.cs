using FluentValidation;
using Kharasana.Application.DTOs.Auth;

namespace Kharasana.Application.Validators.Auth;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.EmailOrPhone)
            .NotEmpty().WithMessage("يجب إدخال البريد الإلكتروني أو رقم الهاتف.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}