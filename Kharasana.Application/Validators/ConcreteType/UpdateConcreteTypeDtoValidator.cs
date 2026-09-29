using FluentValidation;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.ConcreteType;

public class UpdateConcreteTypeDtoValidator : AbstractValidator<UpdateConcreteTypeDto>
{
    public UpdateConcreteTypeDtoValidator()
    {
        RuleFor(x => x.Name).ConcreteTypeName();
        RuleFor(x => x.Strength).ConcreteTypeStrength();
        RuleFor(x => x.UnitPrice).ConcreteTypeUnitPrice();
    }
}