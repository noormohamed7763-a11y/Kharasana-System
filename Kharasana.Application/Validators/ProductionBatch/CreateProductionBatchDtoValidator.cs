using FluentValidation;
using Kharasana.Application.DTOs.ProductionBatch;

namespace Kharasana.Application.Validators.ProductionBatch;

public class CreateProductionBatchDtoValidator : AbstractValidator<CreateProductionBatchDto>
{
    public CreateProductionBatchDtoValidator()
    {
        RuleFor(x => x.ConcreteTypeId).GreaterThan(0).WithMessage("يجب تحديد نوع الخرسانة.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("يجب أن تكون الكمية أكبر من صفر.");
    }
}
