using Kharasana.Application.DTOs.Factory;

namespace Kharasana.Application.Interfaces.Services;

public interface IFactoryService
{
    Task<IEnumerable<FactoryDto>> GetAllAsync();
    Task<IEnumerable<FactoryDto>> GetArchivedAsync();
    Task<FactoryDto?> GetByIdAsync(int id);
    Task<FactoryDto> CreateAsync(CreateFactoryDto dto);
    Task<bool> UpdateAsync(int id, UpdateFactoryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);

    // ✅ جديد: إدارة شعار المصنع
    Task<string> UploadLogoAsync(int id, Stream fileStream, string originalFileName, long fileLength);
    Task<bool> DeleteLogoAsync(int id);
}