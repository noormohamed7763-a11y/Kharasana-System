using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.FactoryRegistration;

public class RegistrationRequestDetailsViewModel
{
    public int Id { get; set; }
    public string FactoryName { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}