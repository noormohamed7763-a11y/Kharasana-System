using FluentValidation;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class RejectOrderDtoValidator : AbstractValidator<RejectOrderDto>
{
    public RejectOrderDtoValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("سبب الرفض لا يجب أن يتجاوز 500 حرف.");
    }
}