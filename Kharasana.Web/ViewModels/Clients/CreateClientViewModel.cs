using Kharasana.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Clients;

public class CreateClientViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "البريد الإلكتروني")]
    [YemeniEmail(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
    public string? Email { get; set; }

    [Display(Name = "رقم الهاتف")]
    [YemeniPhone(ErrorMessage = "رقم الهاتف غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
    public string? Phone { get; set; }

    [Display(Name = "واتساب")]
    [YemeniPhone(ErrorMessage = "رقم الواتساب غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
    public string? WhatsApp { get; set; }

    [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
    [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 أحرف.")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;
}