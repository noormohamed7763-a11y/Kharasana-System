using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Order;

/// <summary>
/// التحقق من بيانات الدفعة قبل تسجيلها.
///
/// <para>السقوف النصّية (256 للمرجع، 1000 للملاحظات) مطابقة تماماً لقيود
/// <c>HasMaxLength</c> في <c>OrderPaymentConfiguration</c> — لأن النصّ الأطول
/// يصل إلى SQL Server فيرمي الخطأ 8152 (اقتطاع) فيصير الردّ 500 بدل 400.</para>
/// </summary>
public class AddPaymentDtoValidator : AbstractValidator<AddPaymentDto>
{
    public AddPaymentDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(Messages.PaymentAmountMustBePositive);

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("طريقة الدفع غير صالحة.");

        RuleFor(x => x.ReferenceNo)
            .CappedAt(256, Messages.PaymentReferenceMaxLength);

        RuleFor(x => x.Notes)
            .CappedAt(1000, Messages.PaymentNotesMaxLength);
    }
}
