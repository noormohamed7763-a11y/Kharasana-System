namespace Kharasana.Web.Models.Components;

public class SummaryCardViewModel
{
    public string Title { get; set; } = "";

    public string Value { get; set; } = "0";

    public string Icon { get; set; } = "bi-circle";

    public string IconClass { get; set; } = "kpi-icon-primary";

    /// <summary>
    /// رابط اختياري — عند تعيينه تصبح البطاقة قابلة للنقر (kpi-card--link).
    /// يُبنى عادةً عبر @Url.Action في الصفحة (مثلاً قائمة مفيتَرة بالحالة).
    /// </summary>
    public string? LinkUrl { get; set; }
}