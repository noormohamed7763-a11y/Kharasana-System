namespace Kharasana.API.DTOs.Auth;

public class LoginRequestDto
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}