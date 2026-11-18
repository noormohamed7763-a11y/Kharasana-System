using System.Text.Json.Serialization;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Users;

public class UserDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("whatsApp")]
    public string? WhatsApp { get; set; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("licenseNumber")]
    public string? LicenseNumber { get; set; }

    [JsonPropertyName("driverStatus")]
    public DriverStatus? DriverStatus { get; set; }

    [JsonPropertyName("factoryId")]
    public int? FactoryId { get; set; }

    [JsonPropertyName("factoryName")]
    public string? FactoryName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
