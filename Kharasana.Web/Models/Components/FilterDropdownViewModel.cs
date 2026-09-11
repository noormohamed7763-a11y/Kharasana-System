namespace Kharasana.Web.Models.Components;

public class FilterOptionViewModel
{
    public string Text { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class FilterDropdownViewModel
{
    public string Id { get; set; } = "filter";

    public string Label { get; set; } = "تصفية";

    public List<FilterOptionViewModel> Options { get; set; } = new();

    public string SelectedValue { get; set; } = string.Empty;

    public string OnChangeFunction { get; set; } = string.Empty;

    public string CssClass { get; set; } = string.Empty;
}