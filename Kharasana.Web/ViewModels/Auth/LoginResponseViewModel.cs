namespace Kharasana.Web.ViewModels.Auth
{
    public class LoginResponseViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int? FactoryId { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime Expiration { get; set; }

        public string? Notification { get; set; }

        public bool? FactoryIsActive { get; set; }
    }
}