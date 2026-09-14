using Kharasana.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.Factories
{
    public class FactoryFormViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المصنع.")]
        [Display(Name = "اسم المصنع")]
        public string FactoryName { get; set; } = string.Empty;

        [Display(Name = "اسم المالك")]
        public string? OwnerName { get; set; }

        [YemeniPhone(ErrorMessage = "رقم الهاتف غير صحيح. أدخل رقماً يمنياً صالحاً (مثال: 771234567).")]
        [Display(Name = "رقم الهاتف")]
        public string? Phone { get; set; }

        [Display(Name = "رقم الواتساب")]
        public string? WhatsApp { get; set; }

        [YemeniEmail(ErrorMessage = "البريد الإلكتروني غير صحيح.")]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "يرجى إدخال المنطقة.")]
        [Display(Name = "المنطقة")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال العنوان التفصيلي.")]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "خط العرض")]
        public decimal? Latitude { get; set; }

        [Display(Name = "خط الطول")]
        public decimal? Longitude { get; set; }

        [Display(Name = "رابط الشعار")]
        public string? Logo { get; set; }

        [Display(Name = "حالة التشغيل")]
        public bool IsActive { get; set; } = true;
    }
}