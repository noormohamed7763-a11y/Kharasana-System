using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Report;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetAllWithDetailsAsync();
    Task<Order?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Order>> GetAllByFactoryIdAsync(int factoryId);
    Task<IEnumerable<Order>> GetAllByClientIdAsync(int clientId);

    Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, OrderStatus? status, string? search, int pageNumber, int pageSize);

    Task<(IEnumerable<CustomerSummaryDto> Items, int TotalCount)> GetFactoryCustomersAsync(
        int? factoryId, string? search, int pageNumber, int pageSize);

    /// <summary>Reports: order count grouped by status.</summary>
    Task<List<OrderStatusCountDto>> GetCountByStatusAsync(int? factoryId);

    /// <summary>Reports: order count grouped by concrete type.</summary>
    Task<List<ConcreteTypeCountDto>> GetCountByConcreteTypeAsync(int? factoryId);
}