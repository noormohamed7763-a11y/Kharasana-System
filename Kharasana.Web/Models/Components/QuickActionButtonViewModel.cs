namespace Kharasana.Web.Models.Components
{
    public class QuickActionButtonViewModel
    {
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string CssClass { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        // اجعلها من هذا النوع مباشرة لتفادي التحويلات الضمنية
        public IDictionary<string, string>? RouteValues { get; set; }
    }
}