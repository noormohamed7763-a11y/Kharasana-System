using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Validators.Order;

public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderDtoValidator()
    {
        RuleFor(x => x.ConcreteTypeId).GreaterThan(0).WithMessage(Messages.ConcreteTypeRequired);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(Messages.QuantityMustBePositive);
        RuleFor(x => x.Quantity).LessThanOrEqualTo(1000).WithMessage(Messages.QuantityTooLarge);
        RuleFor(x => x.TransportMethod).IsInEnum().WithMessage(Messages.TransportMethodInvalid);
        RuleFor(x => x.SlabType).IsInEnum().WithMessage(Messages.SlabTypeInvalid);

        // ✅ سقوف النصوص الحرة = HasMaxLength للأعمدة في OrderConfiguration
        //    (200/200/100/500/1000) — تمنع الخطأ 8152 وتحوّل الرد إلى 400.
        RuleFor(x => x.ProjectName)
            .MaximumLength(200).WithMessage(Messages.ProjectNameMaxLength);
        RuleFor(x => x.ProjectOwnerName)
            .MaximumLength(200).WithMessage(Messages.ProjectOwnerNameMaxLength);
        RuleFor(x => x.SiteArea)
            .MaximumLength(100).WithMessage(Messages.SiteAreaMaxLength);
        RuleFor(x => x.SiteDescription)
            .MaximumLength(500).WithMessage(Messages.SiteDescriptionMaxLength);
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage(Messages.NotesMaxLength);

        RuleFor(x => x.PouringDate)
            .Must(date => date is null || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage(Messages.PouringDateCannotBeInPast);

        RuleFor(x => x.FloorNumber)
            .NotNull().When(x => x.NeedPump)
            .WithMessage(Messages.PumpRequiresFloorNumber);

        RuleFor(x => x.FloorNumber)
            .GreaterThanOrEqualTo(0).When(x => x.NeedPump && x.FloorNumber.HasValue)
            .WithMessage(Messages.FloorNumberMustBeNonNegative);
    }
}