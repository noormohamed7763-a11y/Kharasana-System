using FluentValidation;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Factory;

public class UpdateFactoryDtoValidator : AbstractValidator<UpdateFactoryDto>
{
    public UpdateFactoryDtoValidator()
    {
        RuleFor(x => x.FactoryName).NotEmpty().WithMessage("اسم المصنع مطلوب.").MaximumLength(200);
        RuleFor(x => x.Area).NotEmpty().WithMessage("المنطقة مطلوبة.").MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().WithMessage("العنوان مطلوب.").MaximumLength(300);
        RuleFor(x => x.OwnerName).MaximumLength(200);

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("البريد الإلكتروني غير صالح.");
    }
}