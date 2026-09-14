using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;

namespace Kharasana.Application.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    // ✅ تم إزالة [Required] وجعل البريد الإلكتروني اختيارياً
    [YemeniEmail(ErrorMessage = "البريد الإلكتروني غير صالح.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
    [MinLength(6, ErrorMessage = Messages.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب.")]
    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // ✅ التحقق من صيغة الهاتف يتم عبر RegisterClientValidator (FluentValidation) بقاعدة YemeniPhoneHelper
    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    // ✅ تم إزالة Role و FactoryId لأنهما ليسا جزءاً من التسجيل الذاتي للعميل
}