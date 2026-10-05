using FluentValidation;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Factory;

public class RegisterFactoryDtoValidator : AbstractValidator<RegisterFactoryDto>
{
    public RegisterFactoryDtoValidator()
    {
        RuleFor(x => x.FactoryName).FactoryName();
        RuleFor(x => x.OwnerName).FactoryOwnerName();
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Phone).YemeniPhone();
        RuleFor(x => x.BusinessRegistrationNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Area).FactoryArea();
        RuleFor(x => x.Address).FactoryAddress();
    }
}