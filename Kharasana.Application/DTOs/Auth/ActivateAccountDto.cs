namespace Kharasana.Application.DTOs.Auth;

public class ActivateAccountDto
{
    public string TokenHash { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}