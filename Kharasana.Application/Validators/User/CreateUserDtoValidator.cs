using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Validators.User;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FullName).FullName();

        // ✅ سقف 256 للبريد كان غائباً هنا (كما في التسجيل الذاتي): نصّ أطول من العمود
        //    يرمي الخطأ 8152 فيصير الرد 500 بدل 400. القاعدة الآن من ValidationRules.Email.
        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.Role)
            .Role()
            .NotEqual(UserRole.Admin).WithMessage(Messages.CannotCreateAdmin);
    }
}