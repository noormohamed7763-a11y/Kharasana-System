using Kharasana.Web.ViewModels.Orders;

namespace Kharasana.Web.ViewModels.Users;

/// <summary>
/// نموذج تقرير طلبات سائق — يُعرض في صفحة قابلة للطباعة عبر window.print().
/// </summary>
public class DriverReportViewModel
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? FactoryName { get; set; }

    /// <summary>عدد طلبات السائق كما جُلبت لمحةً عامة في ترويسة التقرير.</summary>
    public int OrderCount { get; set; }

    /// <summary>تاريخ طباعة التقرير.</summary>
    public DateTime ReportDate { get; set; } = DateTime.Now;

    /// <summary>طلبات السائق مرتبة تنازلياً بتاريخ الإنشاء (تُرتّب في الخدمة).</summary>
    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();
}