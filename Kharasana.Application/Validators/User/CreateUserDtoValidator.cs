using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FullName).FullName();

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("الدور غير صالح.")
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotCreateAdmin);
    }
}