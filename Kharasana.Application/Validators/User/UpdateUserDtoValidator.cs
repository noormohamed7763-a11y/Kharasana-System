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
        //    الصيغة والسقف 256 (وسبب السقف: الخطأ 8152) في ValidationRules.Email.
        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Role)
            .Role()
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotChangeToAdmin);
    }
}