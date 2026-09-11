using FluentValidation;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("حالة الطلب غير صالحة.");
    }
}