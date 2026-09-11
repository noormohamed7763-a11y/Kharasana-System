using FluentValidation;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class AssignDriverDtoValidator : AbstractValidator<AssignDriverDto>
{
    public AssignDriverDtoValidator()
    {
        RuleFor(x => x.DriverId).GreaterThan(0).WithMessage("يجب تحديد السائق.");
        RuleFor(x => x.TruckPlate)
            .NotEmpty().WithMessage("رقم لوحة الشاحنة مطلوب.")
            .MaximumLength(30);
    }
}