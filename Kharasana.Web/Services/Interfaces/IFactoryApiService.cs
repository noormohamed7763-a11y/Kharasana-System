using Kharasana.Web.Common;
using Kharasana.Web.ViewModels.Factories;
using Microsoft.AspNetCore.Http;

namespace Kharasana.Web.Services.Interfaces
{
    public interface IFactoryApiService
    {
        Task<ServiceResult<List<FactoryListItemViewModel>>> GetAllAsync();
        Task<ServiceResult<List<FactoryListItemViewModel>>> GetArchivedAsync();
        Task<ServiceResult<FactoryDetailsViewModel>> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(CreateFactoryViewModel model);
        Task<ServiceResult> UpdateAsync(int id, UpdateFactoryViewModel model);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ServiceResult> RestoreAsync(int id);

        /// <summary>رفع شعار المصنع — يعيد مسار الشعار الجديد على النجاح.</summary>
        Task<ServiceResult<string>> UploadLogoAsync(int id, IFormFile file);
        Task<ServiceResult> DeleteLogoAsync(int id);
    }
}
