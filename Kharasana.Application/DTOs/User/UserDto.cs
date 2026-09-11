using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.User;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? ProfileImage { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }
    public bool IsActive { get; set; }
}