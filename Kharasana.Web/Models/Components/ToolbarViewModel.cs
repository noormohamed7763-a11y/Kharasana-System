namespace Kharasana.Web.Models.Components;

public class ToolbarViewModel
{
    public bool ShowAddButton { get; set; } = true;
    public string AddButtonText { get; set; } = "إضافة جديد";
    public string AddButtonAction { get; set; } = "Create";
    public string AddButtonController { get; set; } = string.Empty;
    public bool ShowExport { get; set; } = false;
    public string ExportActionScript { get; set; } = string.Empty;
}