namespace Kharasana.Application.DTOs.Factory;

public class UpdateFactoryDto
{
    public string FactoryName { get; set; } = string.Empty;

    public string? OwnerName { get; set; }

    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    public string? Email { get; set; }

    public string Area { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Logo { get; set; }

    /// <summary>
    /// تشغيل/إيقاف المصنع — <b>يُعدَّل فقط حين يُرسَل صراحةً</b>.
    /// كان <c>bool</c> غير قابل للقيم الفارغة: جسم PUT لا يحوي <c>isActive</c>
    /// (أو يحمله <c>null</c> في JSON) كان يُسند <c>false</c> فيُوقف المصنع
    /// <b>صامتًا</b> دون قصد. الويب سليم لأن الـcheckbox يُرسله دائمًا،
    /// لكن عقد الـAPI كان فخًا لكل مستدعٍ آخر. الآن <c>null</c> = إبقاء
    /// الحالة الحالية دون تغيير.
    /// </summary>
    public bool? IsActive { get; set; }
}