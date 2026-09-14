using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Validators.Order;

public class PhoneOrderDtoValidator : AbstractValidator<PhoneOrderDto>
{
    public PhoneOrderDtoValidator()
    {
        RuleFor(x => x.ClientPhone)
            .NotEmpty().WithMessage("رقم هاتف العميل مطلوب.")
            .Must(YemeniPhoneHelper.IsValid)
            .WithMessage(Messages.InvalidYemeniPhone);

        RuleFor(x => x.FactoryId).GreaterThan(0).WithMessage("يجب تحديد المصنع.");
        RuleFor(x => x.ConcreteTypeId).GreaterThan(0).WithMessage("يجب تحديد نوع الخرسانة.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر.");
        RuleFor(x => x.TransportMethod).IsInEnum().WithMessage("طريقة النقل غير صالحة.");
        RuleFor(x => x.SlabType).IsInEnum().WithMessage("نوع الصبة غير صالح.");
    }
}