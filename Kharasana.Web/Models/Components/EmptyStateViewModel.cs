namespace Kharasana.Web.Models.Components;

public class EmptyStateViewModel
{
    public string Icon { get; set; } = "bi-inbox";

    /// <summary>
    /// مسار صورة اختياري يُعرض بدل الأيقونة في الحالة الفارغة.
    /// إذا تُرك فارغًا تظهر الأيقونة كما في السابق (توافق رجعي كامل).
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    public string Title { get; set; } = "لا توجد بيانات";

    public string Message { get; set; } = "لا توجد بيانات متاحة للعرض.";

    public bool ShowActionButton { get; set; }

    public string ActionText { get; set; } = "إضافة جديد";

    public string ActionController { get; set; } = string.Empty;

    public string ActionAction { get; set; } = "Create";
}