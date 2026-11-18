namespace Kharasana.Web.ViewModels.Auth;

public class ActivateAccountViewModel
{
    public string TokenHash { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}