using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.WhatsApp)
            .Must(YemeniPhoneHelper.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp))
            .WithMessage(Messages.InvalidWhatsAppNumber);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("الدور غير صالح.")
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotChangeToAdmin);
    }
}