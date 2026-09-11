using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Enums;

/// <summary>
/// طريقة نقل الخرسانة
/// </summary>
public enum TransportMethod
{
    /// <summary>
    /// يتم النقل بواسطة سيارات المصنع
    /// </summary>
    [Display(Name = "🚛 نقل عبر المصنع",
              Description = "يقوم المصنع بتوفير سيارات النقل")]
    FactoryTransport = 0,

    /// <summary>
    /// يتم النقل بواسطة العميل (لديه طريقة نقل خاصة)
    /// </summary>
    [Display(Name = "🚗 نقل ذاتي (العميل)",
              Description = "العميل لديه وسيلة نقل خاصة")]
    ClientOwnTransport = 1
}