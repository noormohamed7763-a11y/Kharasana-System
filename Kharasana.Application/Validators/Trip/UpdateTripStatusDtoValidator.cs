using FluentValidation;
using Kharasana.Application.DTOs.Trips;

namespace Kharasana.Application.Validators.Trip;

public class UpdateTripStatusDtoValidator : AbstractValidator<UpdateTripStatusDto>
{
    public UpdateTripStatusDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("حالة غير صالحة.");
    }
}
