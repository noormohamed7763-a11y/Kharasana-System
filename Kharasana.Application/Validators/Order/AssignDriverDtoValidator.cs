using FluentValidation;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Order;

public class AssignDriverDtoValidator : AbstractValidator<AssignDriverDto>
{
    public AssignDriverDtoValidator()
    {
        RuleFor(x => x.DriverId).DriverId();
        RuleFor(x => x.TruckPlate).TruckPlate();
    }
}