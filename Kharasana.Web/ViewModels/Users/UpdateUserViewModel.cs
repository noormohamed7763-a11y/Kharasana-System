using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Users;

public class UpdateUserViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
    // ✅ 200 لا 150 — نفس سبب CreateUserViewModel: مطابقةُ العمود
    //    (Users.FullName = nvarchar(200)) وقاعدة ValidationRules.FullName().
    [StringLength(200, ErrorMessage = "الاسم الكامل يجب ألا يزيد عن 200 حرف.")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [YemeniEmail(ErrorMessage = "البريد الإلكتروني غير صحيح.")]
    [Display(Name = "البريد الإلكتروني")]
    public string? Email { get; set; }

    [YemeniPhone(ErrorMessage = "رقم الهاتف غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
    [Display(Name = "رقم الهاتف")]
    public string? Phone { get; set; }

    [Display(Name = "رقم الواتساب")]
    [YemeniPhone(ErrorMessage = "رقم الواتساب غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
    public string? WhatsApp { get; set; }

    [Required(ErrorMessage = "يجب تحديد نوع المستخدم.")]
    [Display(Name = "نوع المستخدم")]
    public UserRole Role { get; set; }

    [Display(Name = "رقم الرخصة")]
    public string? LicenseNumber { get; set; }

    [Display(Name = "حالة السائق")]
    public DriverStatus? DriverStatus { get; set; }

    [Display(Name = "المصنع")]
    public int? FactoryId { get; set; }

    [Display(Name = "الحالة")]
    public bool IsActive { get; set; }
}