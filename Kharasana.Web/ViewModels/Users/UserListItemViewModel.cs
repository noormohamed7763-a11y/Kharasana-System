namespace Kharasana.Web.Models.Users;

public class UserListItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? FactoryId { get; set; }
    public string? FactoryName { get; set; }  // ✅ أضف هذا
    public bool IsActive { get; set; }
    public string? LicenseNumber { get; set; }
    public string? DriverStatus { get; set; }
}