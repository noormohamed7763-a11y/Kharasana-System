using System.ComponentModel.DataAnnotations;
using Kharasana.Web.Models.ConcreteCatalog;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.ViewModels.ConcreteTypes;

public class CreateConcreteTypeViewModel
{
    // =========================
    // المصنع
    // =========================

    [Display(Name = "المصنع")]
    public int FactoryId { get; set; }

    public List<SelectListItem> Factories { get; set; } = new();


    // =========================
    // الأنواع القياسية
    // =========================

    [Display(Name = "نوع الخرسانة")]
    public string? SelectedConcreteCode { get; set; }

    /// <summary>كتالوج الأنواع القياسية — يُستخدم في قائمة الاختيار وفي بيانات المعاينة (data-strength/data-usage).</summary>
    public IEnumerable<ConcreteStandardModel> ConcreteCatalog { get; set; }
        = Enumerable.Empty<ConcreteStandardModel>();


    // =========================
    // النوع المخصص
    // =========================

    public bool IsCustomType { get; set; }

    [Display(Name = "اسم النوع")]
    public string? CustomName { get; set; }

    [Display(Name = "المقاومة (MPa)")]
    [Range(1, 100, ErrorMessage = "أدخل قيمة صحيحة.")]
    public int? CustomStrength { get; set; }


    // =========================
    // الحقول التي ترسل إلى الـ API
    // =========================

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Strength { get; set; }


    // =========================
    // بيانات السعر
    // =========================

    [Required(ErrorMessage = "سعر المتر المكعب مطلوب.")]
    [Range(0.01, 100000000, ErrorMessage = "أدخل سعراً صحيحاً.")]
    [Display(Name = "سعر المتر المكعب (ر.ي)")]
    public decimal UnitPrice { get; set; }


    // =========================
    // الصورة والوصف
    // =========================

    [Display(Name = "رابط الصورة")]
    public string? ImageUrl { get; set; }

    [Display(Name = "الوصف")]
    public string? Description { get; set; }
}