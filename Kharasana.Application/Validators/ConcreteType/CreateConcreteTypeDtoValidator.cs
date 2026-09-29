using FluentValidation;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.ConcreteType;

public class CreateConcreteTypeDtoValidator : AbstractValidator<CreateConcreteTypeDto>
{
    public CreateConcreteTypeDtoValidator()
    {
        RuleFor(x => x.FactoryId).FactoryId();
        RuleFor(x => x.Name).ConcreteTypeName();
        RuleFor(x => x.Strength).ConcreteTypeStrength();
        RuleFor(x => x.UnitPrice).ConcreteTypeUnitPrice();
    }
}