using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Validators.Common;

namespace Kharasana.Application.Validators.Auth;

/// <summary>
/// تحقق التسجيل الذاتي للعميل — مُشغَّل عبر ValidationFilter على AuthController.Register.
/// </summary>
public class RegisterClientValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterClientValidator()
    {
        RuleFor(x => x.FullName).FullName();

        // ✅ البريد اختياري في التسجيل الذاتي (الهاتف هو المُعرّف الأساسي)،
        //    و DTO نفسه يستخدم [YemeniEmail] الذي يعتبر الفارغ صالحاً.
        //    كانت هنا NotEmpty() فتتعارض مع قرار جعل البريد اختيارياً وتُفشل
        //    تسجيل أي عميل بلا بريد — صُحّحت لتطابق نية الـ DTO.
        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Password).Password();

        // ✅ رسالة عدم التطابق من الرسائل المشتركة — تطابق ما ترميه AuthService
        //    (كان هنا نصّ مختلف قليلاً: «كلمتا المرور غير متطابقتين.»)
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(Messages.ConfirmPasswordRequired)
            .Equal(x => x.Password).WithMessage(Messages.PasswordsNotMatch);

        RuleFor(x => x.Phone).YemeniPhone();

        RuleFor(x => x.WhatsApp).YemeniWhatsApp();
    }
}