using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Kharasana.Web.ViewModels.ConcreteTypes;

public class UpdateConcreteTypeViewModel
{
    public int ConcreteTypeId { get; set; }

    [Required(ErrorMessage = "اسم نوع الخرسانة مطلوب.")]
    [Display(Name = "اسم النوع")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "مقاومة الضغط مطلوبة.")]
    [Range(1, 100, ErrorMessage = "أدخل قيمة صحيحة.")]
    [Display(Name = "مقاومة الضغط (MPa)")]
    public int Strength { get; set; }

    [Required(ErrorMessage = "سعر المتر المكعب مطلوب.")]
    [Range(0.01, 100000000, ErrorMessage = "أدخل سعراً صحيحاً.")]
    [Display(Name = "سعر المتر المكعب (ر.ي)")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "رابط الصورة")]
    public string? ImageUrl { get; set; }

    [Display(Name = "الوصف")]
    public string? Description { get; set; }

    [Display(Name = "الحالة")]
    public bool IsActive { get; set; }
}