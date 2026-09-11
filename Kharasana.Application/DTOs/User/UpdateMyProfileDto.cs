namespace Kharasana.Application.DTOs.User;

public class UpdateMyProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
}