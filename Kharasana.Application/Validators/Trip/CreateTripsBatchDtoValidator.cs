using FluentValidation;
using Kharasana.Application.DTOs.Trips;

namespace Kharasana.Application.Validators.Trip;

public class CreateTripsBatchDtoValidator : AbstractValidator<CreateTripsBatchDto>
{
    public CreateTripsBatchDtoValidator()
    {
        RuleFor(x => x.Trips).NotEmpty().WithMessage("يجب إرسال قائمة رحلات واحدة على الأقل.");
        RuleForEach(x => x.Trips).SetValidator(new CreateTripDtoValidator());
    }
}
