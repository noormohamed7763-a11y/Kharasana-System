using Kharasana.Application.Common;

namespace Kharasana.Web.ViewModels.Drivers;

public class DriversIndexViewModel
{
    public PagedResult<DriverListItemViewModel>? PagedDrivers { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}