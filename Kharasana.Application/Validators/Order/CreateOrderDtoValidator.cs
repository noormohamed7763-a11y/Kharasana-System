using FluentValidation;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.FactoryId).GreaterThan(0).WithMessage("يجب تحديد المصنع.");
        RuleFor(x => x.ConcreteTypeId).GreaterThan(0).WithMessage("يجب تحديد نوع الخرسانة.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر.");
        RuleFor(x => x.TransportMethod).IsInEnum().WithMessage("طريقة النقل غير صالحة.");
        RuleFor(x => x.SlabType).IsInEnum().WithMessage("نوع الصبة غير صالح.");
    }
}