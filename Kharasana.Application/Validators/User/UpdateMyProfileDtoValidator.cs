using FluentValidation;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.User;

public class UpdateMyProfileDtoValidator : AbstractValidator<UpdateMyProfileDto>
{
    public UpdateMyProfileDtoValidator()
    {
        RuleFor(x => x.FullName).FullName();

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();
    }
}