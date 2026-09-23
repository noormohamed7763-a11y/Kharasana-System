using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderDtoValidator()
    {
        RuleFor(x => x.ConcreteTypeId).GreaterThan(0).WithMessage(Messages.ConcreteTypeRequired);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(Messages.QuantityMustBePositive);
        RuleFor(x => x.Quantity).LessThanOrEqualTo(1000).WithMessage(Messages.QuantityTooLarge);
        RuleFor(x => x.TransportMethod).IsInEnum().WithMessage(Messages.TransportMethodInvalid);
        RuleFor(x => x.SlabType).IsInEnum().WithMessage(Messages.SlabTypeInvalid);

        RuleFor(x => x.PouringDate)
            .Must(date => date is null || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage(Messages.PouringDateCannotBeInPast);

        RuleFor(x => x.FloorNumber)
            .NotNull().When(x => x.NeedPump)
            .WithMessage(Messages.PumpRequiresFloorNumber);

        RuleFor(x => x.FloorNumber)
            .GreaterThanOrEqualTo(0).When(x => x.NeedPump && x.FloorNumber.HasValue)
            .WithMessage(Messages.FloorNumberMustBeNonNegative);
    }
}