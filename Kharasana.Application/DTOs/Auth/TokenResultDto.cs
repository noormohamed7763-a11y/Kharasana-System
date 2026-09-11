namespace Kharasana.Application.DTOs.Auth;

public class TokenResultDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }
}