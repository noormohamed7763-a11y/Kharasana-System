namespace Kharasana.Application.DTOs.ConcreteType;

public class ConcreteTypeDto
{
    public int ConcreteTypeId { get; set; }

    public int FactoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public string FactoryName { get; set; } = string.Empty;

    /// <summary>
    /// آخر تعديل — وفي النوع <b>المؤرشف</b> هو لحظة الأرشفة نفسها، لأن
    /// <c>DeleteAsync</c> وحدها تكتبه بعد آخر تعديل ولا يُعدَّل الصف وهو مؤرشف
    /// (بقية العمليات تمرّ بـ <c>GetByIdAsync</c> الذي يستبعده). تُعرض في صفحة الأرشيف.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}