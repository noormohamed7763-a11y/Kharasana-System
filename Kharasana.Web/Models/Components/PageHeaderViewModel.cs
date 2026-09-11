namespace Kharasana.Web.Models.Components
{
    public class PageHeaderViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string? ActionText { get; set; }
        public string? ActionController { get; set; }
        public string? ActionMethod { get; set; }
    }
}