using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Validators.User;

public class UpdateMyProfileDtoValidator : AbstractValidator<UpdateMyProfileDto>
{
    public UpdateMyProfileDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(200).WithMessage(Messages.NameMaxLength);

        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || YemeniPhoneHelper.IsValid(phone))
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.WhatsApp)
            .Must(whatsApp => string.IsNullOrWhiteSpace(whatsApp) || YemeniPhoneHelper.IsValid(whatsApp))
            .WithMessage(Messages.InvalidWhatsAppNumber);
    }
}