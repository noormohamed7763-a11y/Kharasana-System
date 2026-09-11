namespace Kharasana.Application.DTOs.Dashboard;

public class FactoryDashboardDto
{
    public int NewOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int OnTheWayOrders { get; set; }
    public int DeliveredToday { get; set; }
    public int AvailableDrivers { get; set; }
}