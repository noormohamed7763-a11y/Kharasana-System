namespace Kharasana.Web.Models.Components;

public class ActionButtonsViewModel
{
    public int Id { get; set; }  // ✅ changed from object to int
    public string ControllerName { get; set; } = string.Empty;

    public bool ShowDetails { get; set; } = true;
    public bool ShowEdit { get; set; } = true;
    public bool ShowDelete { get; set; } = true;

    public string DetailsAction { get; set; } = "Details";
    public string EditAction { get; set; } = "Edit";
    public string DeleteAction { get; set; } = "Delete";

    public string DetailsTooltip { get; set; } = "عرض التفاصيل";
    public string EditTooltip { get; set; } = "تعديل";
    public string DeleteTooltip { get; set; } = "حذف";
}