using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Validators.Common;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Validators.Order;

public class PhoneOrderDtoValidator : AbstractValidator<PhoneOrderDto>
{
    public PhoneOrderDtoValidator()
    {
        RuleFor(x => x.ClientPhone)
            .NotEmpty().WithMessage(Messages.ClientPhoneRequired)
            .Must(YemeniPhoneHelper.IsValid)
            .WithMessage(Messages.InvalidYemeniPhone);

        // ✅ نفس قواعد CreateOrderDtoValidator من المصدر الواحد — كانت هنا نصوصاً صريحة
        //    وثمّة ثوابت، أي نسختين من القاعدة الواحدة قابلتين للانحراف.
        RuleFor(x => x.FactoryId).FactoryId();
        RuleFor(x => x.ConcreteTypeId).ConcreteTypeId();
        RuleFor(x => x.Quantity).Quantity();
        RuleFor(x => x.TransportMethod).TransportMethod();
        RuleFor(x => x.SlabType).SlabType();

        // ✅ سقوف النصوص الحرة = أطوال الأعمدة في OrderConfiguration/UserConfiguration.
        //    كانت غائبة، فالنص الأطول يتجاوز العمود ويرمي SQL Server الخطأ 8152
        //    (اقتطاع) فيصير الرد 500 بدل 400 برسالة عربية.
        //    MaximumLength يتجاهل القيم الفارغة/غير المرسلة فلا حاجة لشرط When.
        RuleFor(x => x.ClientFullName).CappedAt(200, Messages.NameMaxLength);
        RuleFor(x => x.ProjectName).ProjectName();
        RuleFor(x => x.ProjectOwnerName).ProjectOwnerName();
        RuleFor(x => x.SiteArea).SiteArea();
        RuleFor(x => x.SiteDescription).SiteDescription();
        RuleFor(x => x.Notes).Notes();
    }
}