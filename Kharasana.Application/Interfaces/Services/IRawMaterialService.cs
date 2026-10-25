using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Services;

public interface IRawMaterialService
{
    Task<IEnumerable<RawMaterial>> GetFactoryMaterialsAsync(int factoryId);
    Task<RawMaterial> GetByIdAsync(int id, int factoryId);
    Task<RawMaterial> CreateAsync(RawMaterial material, int factoryId);
    Task UpdateStockAsync(int id, decimal quantityChange, int factoryId);
    Task DeleteAsync(int id, int factoryId);
}
