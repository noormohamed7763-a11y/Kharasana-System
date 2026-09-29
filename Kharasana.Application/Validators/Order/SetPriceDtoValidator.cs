using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class SetPriceDtoValidator : AbstractValidator<SetPriceDto>
{
    public SetPriceDtoValidator()
    {
        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage(Messages.UnitPriceMustBePositive);
    }
}