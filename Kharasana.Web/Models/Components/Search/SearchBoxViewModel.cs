namespace Kharasana.Web.Models.Components.Search;

public class SearchBoxViewModel
{
    public string InputId { get; set; } = "globalSearchInput";
    public string Placeholder { get; set; } = "ابحث هنا...";
    public string Value { get; set; } = string.Empty;
    public string OnInputScript { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
}