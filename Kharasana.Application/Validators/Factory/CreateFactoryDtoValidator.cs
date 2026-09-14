using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Validators.Factory;

public class CreateFactoryDtoValidator : AbstractValidator<CreateFactoryDto>
{
    public CreateFactoryDtoValidator()
    {
        RuleFor(x => x.FactoryName).NotEmpty().WithMessage("اسم المصنع مطلوب.").MaximumLength(200);
        RuleFor(x => x.Area).NotEmpty().WithMessage("المنطقة مطلوبة.").MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().WithMessage("العنوان مطلوب.").MaximumLength(300);
        RuleFor(x => x.OwnerName).MaximumLength(200);

        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || YemeniPhoneHelper.IsValid(phone))
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.WhatsApp)
            .Must(whatsApp => string.IsNullOrWhiteSpace(whatsApp) || YemeniPhoneHelper.IsValid(whatsApp))
            .WithMessage(Messages.InvalidWhatsAppNumber);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("البريد الإلكتروني غير صالح.");
    }
}