using Kharasana.Application.Common;

namespace Kharasana.Web.ViewModels.Drivers;

public class DriversIndexViewModel
{
    public PagedResult<DriverListItemViewModel>? PagedDrivers { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>إجمالي السائقين (فعلي عبر كل الصفحات).</summary>
    public int TotalDrivers { get; set; }

    /// <summary>عدد السائقين المتاحين (فعلي عبر كل الصفحات).</summary>
    public int AvailableDrivers { get; set; }

    /// <summary>عدد السائقين المشغولين (فعلي عبر كل الصفحات).</summary>
    public int BusyDrivers { get; set; }

    /// <summary>عدد السائقين غير المتصلين (فعلي عبر كل الصفحات).</summary>
    public int OfflineDrivers { get; set; }
}