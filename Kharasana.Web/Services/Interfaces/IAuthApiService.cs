using Kharasana.Web.ViewModels.Auth;

namespace Kharasana.Web.Services.Interfaces
{
    public interface IAuthApiService
    {
        Task<LoginResponseViewModel?> LoginAsync(LoginViewModel model);

        Task<bool> ActivateAccountAsync(string tokenHash, string newPassword);

        Task LogoutAsync();
    }
}