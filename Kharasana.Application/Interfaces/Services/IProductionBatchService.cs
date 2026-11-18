using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IProductionBatchService
{
    Task<IEnumerable<ProductionBatchDto>> GetFactoryProductionBatchesAsync(UserRole callerRole, int? factoryId);
    Task<ProductionBatchDto> GetByIdAsync(int id, UserRole callerRole, int? callerFactoryId);
    Task<IEnumerable<ProductionBatchDto>> GetByOrderIdAsync(int orderId, UserRole callerRole, int? callerFactoryId);
    Task<ProductionBatchDto> CreateAsync(CreateProductionBatchDto dto, int orderId, UserRole callerRole, int? callerFactoryId);
    Task UpdateStatusAsync(int id, ProductionStatus status, UserRole callerRole, int? callerFactoryId);
    Task DeleteAsync(int id, UserRole callerRole, int? callerFactoryId);
}