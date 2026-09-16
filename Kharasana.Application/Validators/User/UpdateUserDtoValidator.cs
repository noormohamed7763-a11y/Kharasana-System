using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName).FullName();

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("الدور غير صالح.")
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotChangeToAdmin);
    }
}