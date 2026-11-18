using FluentValidation;
using Kharasana.Application.DTOs.Trips;

namespace Kharasana.Application.Validators.Trip;

public class CreateTripDtoValidator : AbstractValidator<CreateTripDto>
{
    public CreateTripDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("يجب أن تكون الكمية أكبر من صفر.");
        RuleFor(x => x.TruckPlate).NotEmpty().WithMessage("رقم اللوحة مطلوب.");
        When(x => x.HasPump, () =>
        {
            RuleFor(x => x.PumpDriverId).NotEmpty().WithMessage("معرّف سائق المضخة مطلوب عند وجود مضخة.");
        });
    }
}
