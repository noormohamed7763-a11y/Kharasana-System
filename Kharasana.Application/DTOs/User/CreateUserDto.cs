using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.User;

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public UserRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }
}