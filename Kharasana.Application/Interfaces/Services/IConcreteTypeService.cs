using Kharasana.Application.DTOs.ConcreteType;

namespace Kharasana.Application.Interfaces.Services;

public interface IConcreteTypeService
{
    Task<IEnumerable<ConcreteTypeDto>> GetAllAsync(int? factoryId = null);
    Task<ConcreteTypeDto> GetByIdAsync(int id, int? currentFactoryId = null);
    Task<ConcreteTypeDto> CreateAsync(CreateConcreteTypeDto dto, int? currentFactoryId = null);
    Task<bool> UpdateAsync(int id, UpdateConcreteTypeDto dto, int? currentFactoryId = null);
    Task<bool> DeleteAsync(int id);
}