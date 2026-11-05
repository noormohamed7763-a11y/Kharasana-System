using FluentValidation;
using Kharasana.Application.DTOs.RawMaterial;

namespace Kharasana.Application.Validators.RawMaterial;

public class CreateRawMaterialDtoValidator : AbstractValidator<CreateRawMaterialDto>
{
    public CreateRawMaterialDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المادة المطلوب.");
        RuleFor(x => x.Unit).NotEmpty().WithMessage("الوحدة مطلوبة.");
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0).WithMessage("الرصيد الحالي يجب ألا يكون سالباً.");
    }
}
