using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName).FullName();

        // ✅ البريد اختياري، وغيابه يعني «أبقِ الحالي» لا «امسحه» (انظر UpdateUserDto.Email).
        //    سقف 256 يطابق UserConfiguration.Email: بدون سقف يصل نصّ أطول إلى العمود
        //    فيرمي SQL Server الخطأ 8152 (اقتطاع) فيصير الرد 500 بدل 400.
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(Messages.InvalidEmail)
            .MaximumLength(256).WithMessage(Messages.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("الدور غير صالح.")
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotChangeToAdmin);
    }
}