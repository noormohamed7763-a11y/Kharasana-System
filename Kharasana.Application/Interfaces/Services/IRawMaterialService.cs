using Kharasana.Application.DTOs.RawMaterial;

namespace Kharasana.Application.Interfaces.Services;

public interface IRawMaterialService
{
    Task<IEnumerable<RawMaterialDto>> GetFactoryMaterialsAsync(int? factoryId);
    Task<RawMaterialDto> GetByIdAsync(int id, int? factoryId);
    Task<RawMaterialDto> CreateAsync(CreateRawMaterialDto dto, int factoryId);
    Task UpdateStockAsync(int id, decimal quantityChange, int? factoryId);
    Task DeleteAsync(int id, int? factoryId);
}
