namespace Kharasana.Web.ViewModels.Users;

public class UserDetailsViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public int? FactoryId { get; set; }
    public string? FactoryName { get; set; }
    public bool IsActive { get; set; }
    public string? LicenseNumber { get; set; }
    public string? DriverStatus { get; set; }
    public string? DriverStatusName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}