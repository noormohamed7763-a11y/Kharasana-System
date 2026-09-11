using FluentValidation;
using Kharasana.Application.DTOs.ConcreteType;

namespace Kharasana.Application.Validators.ConcreteType;

public class CreateConcreteTypeDtoValidator : AbstractValidator<CreateConcreteTypeDto>
{
    public CreateConcreteTypeDtoValidator()
    {
        RuleFor(x => x.FactoryId).GreaterThan(0).WithMessage("يجب تحديد المصنع.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم نوع الخرسانة مطلوب.").MaximumLength(100);
        RuleFor(x => x.Strength).GreaterThan(0).WithMessage("قيمة المقاومة غير صالحة.");
        RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر.");
    }
}