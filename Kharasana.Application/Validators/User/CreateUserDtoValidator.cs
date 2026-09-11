using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.WhatsApp)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp))
            .WithMessage(Messages.InvalidWhatsAppNumber);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(6).WithMessage(Messages.PasswordMinLength);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("الدور غير صالح.")
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotCreateAdmin);
    }
}