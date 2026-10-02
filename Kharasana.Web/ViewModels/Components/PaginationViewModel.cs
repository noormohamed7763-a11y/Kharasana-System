using System.Collections.Generic;

namespace Kharasana.Web.ViewModels.Components;

public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    // هذه الخاصية ستسمح لنا بتمرير أي فلاتر (بحث، حالة، مصنع، إلخ) ديناميكياً
    public Dictionary<string, string> RouteValues { get; set; } = new();
    public string AspAction { get; set; } = "Index";
}
