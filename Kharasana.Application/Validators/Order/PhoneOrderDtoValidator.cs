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

        // ✅ سقوف النصوص الحرة = أطوال الأعمدة في OrderConfiguration/UserConfiguration.
        //    كانت غائبة، فالنص الأطول يتجاوز العمود ويرمي SQL Server الخطأ 8152
        //    (اقتطاع) فيصير الرد 500 بدل 400 برسالة عربية.
        //    MaximumLength يتجاهل القيم الفارغة/غير المرسلة فلا حاجة لشرط When.
        RuleFor(x => x.ClientFullName)
            .MaximumLength(200).WithMessage(Messages.NameMaxLength);
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
    }
}