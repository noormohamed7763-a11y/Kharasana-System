using FluentValidation;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Order;

public class RejectOrderDtoValidator : AbstractValidator<RejectOrderDto>
{
    public RejectOrderDtoValidator()
    {
        RuleFor(x => x.Reason).RejectionReason();
    }
}