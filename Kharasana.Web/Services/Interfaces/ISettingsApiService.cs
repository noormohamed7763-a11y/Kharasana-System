using Kharasana.Web.Models.Settings;
using Microsoft.AspNetCore.Http;

namespace Kharasana.Web.Services.Interfaces
{
    public interface ISettingsApiService
    {
        Task<FactorySettingsViewModel?> GetMySettingsAsync();
        Task<string?> UploadLogoAsync(IFormFile file);
        Task<bool> DeleteLogoAsync();
    }
}