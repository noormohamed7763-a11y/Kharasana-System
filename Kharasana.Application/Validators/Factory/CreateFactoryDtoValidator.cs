using FluentValidation;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Factory;

public class CreateFactoryDtoValidator : AbstractValidator<CreateFactoryDto>
{
    public CreateFactoryDtoValidator()
    {
        RuleFor(x => x.FactoryName).FactoryName();
        RuleFor(x => x.Area).FactoryArea();
        RuleFor(x => x.Address).FactoryAddress();
        RuleFor(x => x.OwnerName).FactoryOwnerName();

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        // ✅ سقف 256 كان غائباً هنا: نصّ أطول من عمود Factories.Email كان يمرّ إلى
        //    SQL Server فيرمي 8152 فيصير الرد 500 بدل 400. القاعدة من ValidationRules.Email.
        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}