using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.User;

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? ProfileImage { get; set; }
    public UserRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }
    public bool IsActive { get; set; }
}