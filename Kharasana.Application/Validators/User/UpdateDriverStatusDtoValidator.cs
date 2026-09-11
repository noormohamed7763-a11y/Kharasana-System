using FluentValidation;
using Kharasana.Application.DTOs.User;

namespace Kharasana.Application.Validators.User;

public class UpdateDriverStatusDtoValidator : AbstractValidator<UpdateDriverStatusDto>
{
    public UpdateDriverStatusDtoValidator()
    {
        RuleFor(x => x.DriverStatus).IsInEnum().WithMessage("حالة السائق غير صالحة.");
    }
}