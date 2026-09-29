using System.ComponentModel.DataAnnotations;
using Kharasana.Application.Common;
using Kharasana.Domain.Validation;

namespace Kharasana.Application.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = Messages.FullNameRequired)]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    // ✅ تم إزالة [Required] وجعل البريد الإلكتروني اختيارياً
    [YemeniEmail(ErrorMessage = Messages.InvalidEmail)]
    public string? Email { get; set; }

    // ✅ الحد الأدنى للطول من المصدر الوحيد PasswordPolicy.MinimumLength — كان 6 هنا
    // ومكتوباً 8 في ValidationRules.Password (ورسالة الخطأ تقول «8»)، فكان التسجيل العام
    // يقبل كلمة مرور أقصر مما تطلبه بقية المسارات. وُحّد الرقم الآن على 8.
    [Required(ErrorMessage = Messages.PasswordRequired)]
    [MinLength(PasswordPolicy.MinimumLength, ErrorMessage = Messages.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = Messages.ConfirmPasswordRequired)]
    // ✅ نصّ عدم التطابق من الرسائل المشتركة: كان هنا «كلمتا المرور غير متطابقتين.»
    //    بينما المُدقّق و AuthService يرميان Messages.PasswordsNotMatch — أي رسالتين
    //    لنفس الخطأ في الطلب الواحد، يتغيّر أيّهما يظهر بحسب أي طبقة تلتقطه أولاً.
    [Compare(nameof(Password), ErrorMessage = Messages.PasswordsNotMatch)]
    public string ConfirmPassword { get; set; } = string.Empty;

    // ✅ التحقق من صيغة الهاتف يتم في AuthService عبر PhoneValidationHelper (بقاعدة YemeniPhoneHelper).
    // ملاحظة: RegisterClientValidator مُشغَّل الآن عبر [ServiceFilter(typeof(ValidationFilter<RegisterUserDto>))]
    // على AuthController.Register — لكنه يبقى تحققاً مساعداً لا مصدر الحقيقة:
    // الخدمة تُعيد فحص تكرار البريد والهاتف بنفسها.
    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    // ✅ تم إزالة Role و FactoryId لأنهما ليسا جزءاً من التسجيل الذاتي للعميل
}