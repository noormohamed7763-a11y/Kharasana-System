namespace Kharasana.Web.ViewModels.FactoryRegistration;

public class RegistrationRequestListItemViewModel
{
    public int Id { get; set; }
    public string FactoryName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}