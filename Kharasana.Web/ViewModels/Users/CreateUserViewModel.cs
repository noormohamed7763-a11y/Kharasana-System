using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Users;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
    // ✅ 200 لا 150: العمود Users.FullName = nvarchar(200) (UserConfiguration:16)،
    //    وValidationRules.FullName() تسقُفه 200 برسالة واحدة (NameMaxLength).
    //    كان 150 هنا أضيق من الاثنين، فيمنع المتصفح اسمًا يقبله الخادم — وبرسالة
    //    تخالف رسالة الخادم لنفس الحقل (رقمان متناقضان).
    [StringLength(200, ErrorMessage = "الاسم الكامل يجب ألا يزيد عن 200 حرف.")]
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = string.Empty;

    [YemeniEmail(ErrorMessage = "البريد الإلكتروني غير صحيح.")]
    [Display(Name = "البريد الإلكتروني")]
    public string? Email { get; set; }

    // ✅ الحد الأدنى من المصدر الوحيد PasswordPolicy.MinimumLength (كان 6، والخدمة تطلب 8)
    [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
    [MinLength(PasswordPolicy.MinimumLength, ErrorMessage = "يجب أن تكون كلمة المرور 8 أحرف على الأقل.")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب.")]
    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;

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
}