using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Drivers;

public class DriverListItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }
    public bool IsActive { get; set; }
}