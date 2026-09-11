using Kharasana.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Drivers;

public class EditDriverViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "البريد الإلكتروني")]
    [YemeniEmail(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
    public string? Email { get; set; }

    [Display(Name = "رقم الهاتف")]
    [YemeniPhone(ErrorMessage = "رقم الهاتف غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
    public string? Phone { get; set; }

    [Display(Name = "رقم الرخصة")]
    public string? LicenseNumber { get; set; }

    public string? ProfileImage { get; set; }

    public int? FactoryId { get; set; }

    [Display(Name = "الحساب مُفعّل")]
    public bool IsActive { get; set; } = true;
}