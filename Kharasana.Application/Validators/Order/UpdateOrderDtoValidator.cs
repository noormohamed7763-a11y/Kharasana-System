using FluentValidation;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Order;

public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderDtoValidator()
    {
        RuleFor(x => x.ConcreteTypeId).ConcreteTypeId();
        RuleFor(x => x.Quantity).Quantity();
        RuleFor(x => x.TransportMethod).TransportMethod();
        RuleFor(x => x.SlabType).SlabType();

        // ✅ سقوف النصوص الحرة = HasMaxLength للأعمدة في OrderConfiguration
        //    (200/200/100/500/1000) — تمنع الخطأ 8152 وتحوّل الرد إلى 400.
        RuleFor(x => x.ProjectName).ProjectName();
        RuleFor(x => x.ProjectOwnerName).ProjectOwnerName();
        RuleFor(x => x.SiteArea).SiteArea();
        RuleFor(x => x.SiteDescription).SiteDescription();
        RuleFor(x => x.Notes).Notes();

        RuleFor(x => x.PouringDate).PouringDate();

        RuleFor(x => x.FloorNumber).PumpFloorNumber(x => x.NeedPump);
    }
}